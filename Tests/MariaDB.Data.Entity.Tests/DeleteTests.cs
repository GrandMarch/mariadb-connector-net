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

using System;
using System.Data;
using System.Threading;
using MariaDB.Data.MySqlClient;
using NUnit.Framework;
using MariaDB.Data.MySqlClient.Tests;
using System.Data.EntityClient;
using System.Data.Common;
using System.Data.Objects;

namespace MariaDB.Data.Entity.Tests
{
    [TestFixture]
    public class DeleteTests : BaseEdmTest
    {
        [Test]
        public void SimpleDeleteAllRows()
        {
            using (testEntities context = new testEntities())
            {
                foreach (Toy t in context.Toys)
                    context.DeleteObject(t);
                context.SaveChanges();

                EntityConnection ec = context.Connection as EntityConnection;
                MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM toys",
                    (MySqlConnection)ec.StoreConnection);
                DataTable dt = new DataTable();
                da.Fill(dt);
                Assert.AreEqual(0, dt.Rows.Count);
            }
        }

        [Test]
        public void SimpleDeleteRowByParameter()
        {
            using (testEntities context = new testEntities())
            {
                MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM toys WHERE minage=3", conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                Assert.IsTrue(dt.Rows.Count > 0);

                ObjectQuery<Toy> toys = context.Toys.Where("it.MinAge = @age", new ObjectParameter("age", 3));
                foreach (Toy t in toys)
                    context.DeleteObject(t);
                context.SaveChanges();

                dt.Clear();
                da.Fill(dt);
                Assert.AreEqual(0, dt.Rows.Count);
            }
        }
    }
}