// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation; version 3 of the License.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License
// for more details.
//
// You should have received a copy of the GNU Lesser General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using NUnit.Framework;
using System.Web.Security;
using System.Collections.Specialized;
using System.Data;
using System;
using System.Configuration.Provider;
using System.Configuration;
using MariaDB.Web.Profile;
using System.Web.Profile;
using System.Reflection;

namespace MariaDB.Web.Tests
{
    [TestFixture]
    public class ProfileTests : BaseWebTest
    {
        private MySQLProfileProvider InitProfileProvider()
        {
            MySQLProfileProvider p = new MySQLProfileProvider();
            NameValueCollection config = new NameValueCollection();
            config.Add("connectionStringName", "LocalMySqlServer");
            config.Add("applicationName", "/");
            p.Initialize(null, config);
            return p;
        }

        [Test]
        public void SettingValuesCreatesAnAppAndUserId()
        {
            MySQLProfileProvider provider = InitProfileProvider();
            SettingsContext ctx = new SettingsContext();
            ctx.Add("IsAuthenticated", false);
            ctx.Add("UserName", "user1");

            SettingsPropertyValueCollection values = new SettingsPropertyValueCollection();
            SettingsProperty property1 = new SettingsProperty("color");
            property1.PropertyType = typeof(string);
            property1.Attributes["AllowAnonymous"] = true;
            SettingsPropertyValue value = new SettingsPropertyValue(property1);
            value.PropertyValue = "blue";
            values.Add(value);

            provider.SetPropertyValues(ctx, values);

            DataTable dt = FillTable("SELECT * FROM my_aspnet_Applications");
            Assert.AreEqual(1, dt.Rows.Count);
            dt = FillTable("SELECT * FROM my_aspnet_Users");
            Assert.AreEqual(1, dt.Rows.Count);
            dt = FillTable("SELECT * FROM my_aspnet_Profiles");
            Assert.AreEqual(1, dt.Rows.Count);

            values["color"].PropertyValue = "green";
            provider.SetPropertyValues(ctx, values);

            dt = FillTable("SELECT * FROM my_aspnet_Applications");
            Assert.AreEqual(1, dt.Rows.Count);
            dt = FillTable("SELECT * FROM my_aspnet_Users");
            Assert.AreEqual(1, dt.Rows.Count);
            dt = FillTable("SELECT * FROM my_aspnet_Profiles");
            Assert.AreEqual(1, dt.Rows.Count);
        }

        [Test]
        public void AnonymousUserSettingNonAnonymousProperties()
        {
            MySQLProfileProvider provider = InitProfileProvider();
            SettingsContext ctx = new SettingsContext();
            ctx.Add("IsAuthenticated", false);
            ctx.Add("UserName", "user1");

            SettingsPropertyValueCollection values = new SettingsPropertyValueCollection();
            SettingsProperty property1 = new SettingsProperty("color");
            property1.PropertyType = typeof(string);
            property1.Attributes["AllowAnonymous"] = false;
            SettingsPropertyValue value = new SettingsPropertyValue(property1);
            value.PropertyValue = "blue";
            values.Add(value);

            provider.SetPropertyValues(ctx, values);

            DataTable dt = FillTable("SELECT * FROM my_aspnet_Applications");
            Assert.AreEqual(0, dt.Rows.Count);
            dt = FillTable("SELECT * FROM my_aspnet_Users");
            Assert.AreEqual(0, dt.Rows.Count);
            dt = FillTable("SELECT * FROM my_aspnet_Profiles");
            Assert.AreEqual(0, dt.Rows.Count);
        }

        [Test]
        public void StringCollectionAsProperty()
        {
            ProfileBase profile = ProfileBase.Create("foo", true);
            ResetAppId(profile.Providers["MySqlProfileProvider"] as MySQLProfileProvider);
            StringCollection colors = new StringCollection();
            colors.Add("red");
            colors.Add("green");
            colors.Add("blue");
            profile["FavoriteColors"] = colors;
            profile.Save();

            DataTable dt = FillTable("SELECT * FROM my_aspnet_Applications");
            Assert.AreEqual(1, dt.Rows.Count);
            dt = FillTable("SELECT * FROM my_aspnet_Users");
            Assert.AreEqual(1, dt.Rows.Count);
            dt = FillTable("SELECT * FROM my_aspnet_Profiles");
            Assert.AreEqual(1, dt.Rows.Count);

            // now retrieve them
            SettingsPropertyCollection getProps = new SettingsPropertyCollection();
            SettingsProperty getProp1 = new SettingsProperty("FavoriteColors");
            getProp1.PropertyType = typeof(StringCollection);
            getProp1.SerializeAs = SettingsSerializeAs.Xml;
            getProps.Add(getProp1);

            MySQLProfileProvider provider = InitProfileProvider();
            SettingsContext ctx = new SettingsContext();
            ctx.Add("IsAuthenticated", true);
            ctx.Add("UserName", "foo");
            SettingsPropertyValueCollection getValues = provider.GetPropertyValues(ctx, getProps);
            Assert.AreEqual(1, getValues.Count);
            SettingsPropertyValue getValue1 = getValues["FavoriteColors"];
            StringCollection outValue = (StringCollection)getValue1.PropertyValue;
            Assert.AreEqual(3, outValue.Count);
            Assert.AreEqual("red", outValue[0]);
            Assert.AreEqual("green", outValue[1]);
            Assert.AreEqual("blue", outValue[2]);
        }

        [Test]
        public void AuthenticatedDateTime()
        {
            ProfileBase profile = ProfileBase.Create("foo", true);
            ResetAppId(profile.Providers["MySqlProfileProvider"] as MySQLProfileProvider);
            DateTime date = DateTime.Now;
            profile["BirthDate"] = date;
            profile.Save();

            SettingsPropertyCollection getProps = new SettingsPropertyCollection();
            SettingsProperty getProp1 = new SettingsProperty("BirthDate");
            getProp1.PropertyType = typeof(DateTime);
            getProp1.SerializeAs = SettingsSerializeAs.Xml;
            getProps.Add(getProp1);

            MySQLProfileProvider provider = InitProfileProvider();
            SettingsContext ctx = new SettingsContext();
            ctx.Add("IsAuthenticated", true);
            ctx.Add("UserName", "foo");

            SettingsPropertyValueCollection getValues = provider.GetPropertyValues(ctx, getProps);
            Assert.AreEqual(1, getValues.Count);
            SettingsPropertyValue getValue1 = getValues["BirthDate"];
            Assert.AreEqual(date, getValue1.PropertyValue);
        }

        /// <summary>
        /// We have to manually reset the app id because our profile provider is loaded from
        /// previous tests but we are destroying our database between tests.  This means that 
        /// our provider thinks we have an application in our database when we really don't.
        /// Doing this will force the provider to generate a new app id.
        /// Note that this is not really a problem in a normal app that is not destroying
        /// the database behind the back of the provider.
        /// </summary>
        /// <param name="p"></param>
        private void ResetAppId(MySQLProfileProvider p)
        {
            Type t = p.GetType();
            FieldInfo fi = t.GetField("app",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.GetField);
            object appObject = fi.GetValue(p);
            Type appType = appObject.GetType();
            PropertyInfo pi = appType.GetProperty("Id");
            pi.SetValue(appObject, -1, null);
        }

        [Test]
        public void AuthenticatedStringProperty()
        {
            ProfileBase profile = ProfileBase.Create("foo", true);
            ResetAppId(profile.Providers["MySqlProfileProvider"] as MySQLProfileProvider);
            profile["Name"] = "Fred Flintstone";
            profile.Save();

            SettingsPropertyCollection getProps = new SettingsPropertyCollection();
            SettingsProperty getProp1 = new SettingsProperty("Name");
            getProp1.PropertyType = typeof(String);
            getProps.Add(getProp1);

            MySQLProfileProvider provider = InitProfileProvider();
            SettingsContext ctx = new SettingsContext();
            ctx.Add("IsAuthenticated", true);
            ctx.Add("UserName", "foo");

            SettingsPropertyValueCollection getValues = provider.GetPropertyValues(ctx, getProps);
            Assert.AreEqual(1, getValues.Count);
            SettingsPropertyValue getValue1 = getValues["Name"];
            Assert.AreEqual("Fred Flintstone", getValue1.PropertyValue);
        }

        /// <summary>
        /// Bug #41654	FindProfilesByUserName error into Connector .NET
        /// </summary>
        [Test]
        public void GetAllProfiles()
        {
            ProfileBase profile = ProfileBase.Create("foo", true);
            ResetAppId(profile.Providers["MySqlProfileProvider"] as MySQLProfileProvider);
            profile["Name"] = "Fred Flintstone";
            profile.Save();

            SettingsPropertyCollection getProps = new SettingsPropertyCollection();
            SettingsProperty getProp1 = new SettingsProperty("Name");
            getProp1.PropertyType = typeof(String);
            getProps.Add(getProp1);

            MySQLProfileProvider provider = InitProfileProvider();
            SettingsContext ctx = new SettingsContext();
            ctx.Add("IsAuthenticated", true);
            ctx.Add("UserName", "foo");

            int total;
            ProfileInfoCollection profiles = provider.GetAllProfiles(
                ProfileAuthenticationOption.All, 0, 10, out total);
            Assert.AreEqual(1, total);
        }
    }
}
