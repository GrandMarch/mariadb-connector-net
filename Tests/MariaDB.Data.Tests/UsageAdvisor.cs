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

using System.Diagnostics;
using NUnit.Framework;

namespace MariaDB.Data.MySqlClient.Tests
{
    [TestFixture]
    public class UsageAdvisorTests : BaseTest
    {
        public UsageAdvisorTests()
        {
            csAdditions = ";Usage Advisor=true;";
        }

        public override void Setup()
        {
            base.Setup();
            createTable("CREATE TABLE Test (id int, name VARCHAR(200))", "INNODB");
        }

        [Test]
        public void NotReadingEveryField()
        {
            execSQL("INSERT INTO Test VALUES (1, 'Test1')");
            execSQL("INSERT INTO Test VALUES (2, 'Test2')");
            execSQL("INSERT INTO Test VALUES (3, 'Test3')");
            execSQL("INSERT INTO Test VALUES (4, 'Test4')");

            GenericListener listener = new GenericListener();

            string sql = "SELECT * FROM Test; SELECT * FROM Test WHERE id > 2";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                reader.Read();
                reader.GetInt32(0);  // access  the first field
                reader.Read();
                Assert.IsTrue(reader.NextResult());
                reader.Read();
                Assert.AreEqual("Test3", reader.GetString(1));
                Assert.IsFalse(reader.NextResult());
            }

            Assert.AreEqual(12, listener.Strings.Count);
            Assert.IsTrue(listener.Strings[0].Contains("Query Opened: SELECT * FROM Test; SELECT * FROM Test WHERE id > 2"));
            Assert.IsTrue(listener.Strings[1].Contains("Resultset Opened: field(s) = 2, affected rows = -1, inserted id = -1"));
            Assert.IsTrue(listener.Strings[2].Contains("Usage Advisor Warning: Query does not use an index"));
            Assert.IsTrue(listener.Strings[3].Contains("Usage Advisor Warning: Skipped 2 rows. Consider a more focused query."));
            Assert.IsTrue(listener.Strings[4].Contains("Usage Advisor Warning: The following columns were not accessed: name"));
            Assert.IsTrue(listener.Strings[5].Contains("Resultset Closed. Total rows=4, skipped rows=2, size (bytes)=32"));
            Assert.IsTrue(listener.Strings[6].Contains("Resultset Opened: field(s) = 2, affected rows = -1, inserted id = -1"));
            Assert.IsTrue(listener.Strings[7].Contains("Usage Advisor Warning: Query does not use an index"));
            Assert.IsTrue(listener.Strings[8].Contains("Usage Advisor Warning: Skipped 1 rows. Consider a more focused query."));
            Assert.IsTrue(listener.Strings[9].Contains("Usage Advisor Warning: The following columns were not accessed: id"));
            Assert.IsTrue(listener.Strings[10].Contains("Resultset Closed. Total rows=2, skipped rows=1, size (bytes)=16"));
            Assert.IsTrue(listener.Strings[11].Contains("Query Closed"));
        }

        [Test]
        public void NotReadingEveryRow()
        {
            execSQL("INSERT INTO Test VALUES (1, 'Test1')");
            execSQL("INSERT INTO Test VALUES (2, 'Test2')");
            execSQL("INSERT INTO Test VALUES (3, 'Test3')");
            execSQL("INSERT INTO Test VALUES (4, 'Test4')");

            GenericListener listener = new GenericListener();

            MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test; SELECT * FROM Test WHERE id > 2", conn);
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                reader.Read();
                reader.Read();
                Assert.IsTrue(reader.NextResult());
                reader.Read();
                reader.Read();
                Assert.IsFalse(reader.NextResult());
            }
            Assert.AreEqual(11, listener.Strings.Count);
            Assert.IsTrue(listener.Strings[0].Contains("Query Opened: SELECT * FROM Test; SELECT * FROM Test WHERE id > 2"));
            Assert.IsTrue(listener.Strings[1].Contains("Resultset Opened: field(s) = 2, affected rows = -1, inserted id = -1"));
            Assert.IsTrue(listener.Strings[2].Contains("Usage Advisor Warning: Query does not use an index"));
            Assert.IsTrue(listener.Strings[3].Contains("Usage Advisor Warning: Skipped 2 rows. Consider a more focused query."));
            Assert.IsTrue(listener.Strings[4].Contains("Usage Advisor Warning: The following columns were not accessed: id,name"));
            Assert.IsTrue(listener.Strings[5].Contains("Resultset Closed. Total rows=4, skipped rows=2, size (bytes)=32"));
            Assert.IsTrue(listener.Strings[6].Contains("Resultset Opened: field(s) = 2, affected rows = -1, inserted id = -1"));
            Assert.IsTrue(listener.Strings[7].Contains("Usage Advisor Warning: Query does not use an index"));
            Assert.IsTrue(listener.Strings[8].Contains("Usage Advisor Warning: The following columns were not accessed: id,name"));
            Assert.IsTrue(listener.Strings[9].Contains("Resultset Closed. Total rows=2, skipped rows=0, size (bytes)=16"));
            Assert.IsTrue(listener.Strings[10].Contains("Query Closed"));
        }

        [Test]
        public void FieldConversion()
        {
            execSQL("INSERT INTO Test VALUES (1, 'Test1')");

            GenericListener listener = new GenericListener();

            MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", conn);
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                reader.Read();
                short s = reader.GetInt16(0);
                long l = reader.GetInt64(0);
                string str = reader.GetString(1);
            }
            Assert.AreEqual(6, listener.Strings.Count);
            Assert.IsTrue(listener.Strings[0].Contains("Query Opened: SELECT * FROM Test"));
            Assert.IsTrue(listener.Strings[1].Contains("Resultset Opened: field(s) = 2, affected rows = -1, inserted id = -1"));
            Assert.IsTrue(listener.Strings[2].Contains("Usage Advisor Warning: Query does not use an index"));
            Assert.IsTrue(listener.Strings[3].Contains("Usage Advisor Warning: The field 'id' was converted to the following types: Int16,Int64"));
            Assert.IsTrue(listener.Strings[4].Contains("Resultset Closed. Total rows=1, skipped rows=0, size (bytes)=8"));
            Assert.IsTrue(listener.Strings[5].Contains("Query Closed"));
        }

        [Test]
        public void NoIndexUsed()
        {
            execSQL("INSERT INTO Test VALUES (1, 'Test1')");
            execSQL("INSERT INTO Test VALUES (2, 'Test1')");
            execSQL("INSERT INTO Test VALUES (3, 'Test1')");
            execSQL("INSERT INTO Test VALUES (4, 'Test1')");

            GenericListener listener = new GenericListener();

            MySqlCommand cmd = new MySqlCommand("SELECT name FROM Test WHERE id=3", conn);
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                reader.Read();
            }
            Assert.AreEqual(6, listener.Strings.Count);
            Assert.IsTrue(listener.Strings[0].Contains("Query Opened: SELECT name FROM Test WHERE id=3"));
            Assert.IsTrue(listener.Strings[1].Contains("Resultset Opened: field(s) = 1, affected rows = -1, inserted id = -1"));
            Assert.IsTrue(listener.Strings[2].Contains("Usage Advisor Warning: Query does not use an index"));
            Assert.IsTrue(listener.Strings[3].Contains("Usage Advisor Warning: The following columns were not accessed: name"));
            Assert.IsTrue(listener.Strings[4].Contains("Resultset Closed. Total rows=1, skipped rows=0, size (bytes)=6"));
            Assert.IsTrue(listener.Strings[5].Contains("Query Closed"));
        }

        [Test]
        public void BadIndexUsed()
        {
            execSQL("DROP TABLE IF EXISTS Test");
            execSQL("CREATE TABLE Test(id INT, name VARCHAR(20) PRIMARY KEY)");
            execSQL("INSERT INTO Test VALUES (1, 'Test1')");
            execSQL("INSERT INTO Test VALUES (2, 'Test2')");
            execSQL("INSERT INTO Test VALUES (3, 'Test3')");
            execSQL("INSERT INTO Test VALUES (4, 'Test4')");

            GenericListener listener = new GenericListener();

            MySqlCommand cmd = new MySqlCommand("SELECT name FROM Test WHERE id=3", conn);
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                reader.Read();
            }
            Assert.AreEqual(6, listener.Strings.Count);
            Assert.IsTrue(listener.Strings[0].Contains("Query Opened: SELECT name FROM Test WHERE id=3"));
            Assert.IsTrue(listener.Strings[1].Contains("Resultset Opened: field(s) = 1, affected rows = -1, inserted id = -1"));
            Assert.IsTrue(listener.Strings[2].Contains("Usage Advisor Warning: Query does not use an index"));
            Assert.IsTrue(listener.Strings[3].Contains("Usage Advisor Warning: The following columns were not accessed: name"));
            Assert.IsTrue(listener.Strings[4].Contains("Resultset Closed. Total rows=1, skipped rows=0, size (bytes)=6"));
            Assert.IsTrue(listener.Strings[5].Contains("Query Closed"));
        }
    }
}