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
using System.Text;
using NUnit.Framework;

namespace MariaDB.Data.MySqlClient.Tests
{
    public class LoggingTests : BaseTest
    {
        public LoggingTests()
        {
            csAdditions = ";logging=true;";
        }

        public override void Setup()
        {
            base.Setup();
            createTable("CREATE TABLE Test (id int, name VARCHAR(200))", "INNODB");
        }

        public void SimpleLogging()
        {
            execSQL("INSERT INTO Test VALUES (1, 'Test1')");
            execSQL("INSERT INTO Test VALUES (2, 'Test2')");
            execSQL("INSERT INTO Test VALUES (3, 'Test3')");
            execSQL("INSERT INTO Test VALUES (4, 'Test4')");

            GenericListener listener = new GenericListener();

            MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", conn);
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
            }
            Assert.AreEqual(4, listener.Strings.Count);
            Assert.IsTrue(listener.Strings[0].Contains("Query Opened: SELECT * FROM Test"));
            Assert.IsTrue(listener.Strings[1].Contains("Resultset Opened: field(s) = 2, affected rows = -1, inserted id = -1"));
            Assert.IsTrue(listener.Strings[2].Contains("Resultset Closed. Total rows=4, skipped rows=4, size (bytes)=32"));
            Assert.IsTrue(listener.Strings[3].Contains("Query Closed"));
        }

        public void Warnings()
        {
            execSQL("DROP TABLE IF EXISTS Test");
            execSQL("CREATE TABLE Test(id INT, name VARCHAR(5))");

            GenericListener listener = new GenericListener();

            MySqlCommand cmd = new MySqlCommand("INSERT IGNORE INTO Test VALUES (1, 'abcdef')", conn);
            cmd.ExecuteNonQuery();

            Assert.AreEqual(9, listener.Strings.Count);
            Assert.IsTrue(listener.Strings[0].Contains("Query Opened: INSERT IGNORE INTO Test VALUES (1, 'abcdef')"));
            Assert.IsTrue(listener.Strings[1].Contains("Resultset Opened: field(s) = 0, affected rows = 1, inserted id = 0"));
            Assert.IsTrue(listener.Strings[2].Contains("Resultset Closed. Total rows=0, skipped rows=0, size (bytes)=0"));
            Assert.IsTrue(listener.Strings[3].Contains("Query Opened: SHOW WARNINGS"));
            Assert.IsTrue(listener.Strings[4].Contains("Resultset Opened: field(s) = 3, affected rows = -1, inserted id = -1"));
            Assert.IsTrue(listener.Strings[5].Contains("Resultset Closed. Total rows=1, skipped rows=0, size (bytes)=55"));
            Assert.IsTrue(listener.Strings[6].Contains("Query Closed"));
            Assert.IsTrue(listener.Strings[7].Contains("MySql Warning: Level=Warning, Code=1265, Message=Data truncated for column 'name' at row 1"));
            Assert.IsTrue(listener.Strings[8].Contains("Query Closed"));
        }

        public void ProviderNormalizingQuery()
        {
            GenericListener listener = new GenericListener();

            StringBuilder sql = new StringBuilder("SELECT '");
            for (int i = 0; i < 400; i++)
                sql.Append("a");
            sql.Append("'");
            MySqlCommand cmd = new MySqlCommand(sql.ToString(), conn);
            cmd.ExecuteNonQuery();

            Assert.AreEqual(5, listener.Strings.Count);
            Assert.IsTrue(listener.Strings[1].EndsWith("SELECT ?"));
        }

        /// <summary>
        /// Bug #57641	Substring out of range exception in ConsumeQuotedToken
        /// </summary>
        public void QuotedTokenAt300()
        {
            GenericListener listener = new GenericListener();

            string sql = @"SELECT 1 AS `AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA1`,  2 AS `AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA2`,
                3 AS `AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA3`,  4 AS `AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA4`,
                5 AS `AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA5`,  6 AS `AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA6`;";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
            }
        }
    }
}