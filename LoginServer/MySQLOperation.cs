using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace LoginServer
{
    public class MySQLOperation
    {
        //private readonly object ReadLock = new object();
        private static readonly object ReadLock = new object();//try this if database concurrent operation is not enabled
        MySqlConnection connection = null;
        public static readonly String databasename = "usersdatabase2";
        public static readonly String tablebasename = "usersinfor2";
        public MySQLOperation()
        {
            //string dbinfor = @"server=localhost;user=root;port=3306;password=41282619900;database=my_db";
            string dbinfor = @"server=localhost;user=root;port=3306;password=41282619900;";
            try
            {
                connection = new MySqlConnection(dbinfor);
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Error: {0}", ex.ToString());

            }
            finally
            {
            }
        }
        public static MySQLOperation getinstance()
        {
                MySQLOperation instance = new MySQLOperation();
                bool b = instance.opendatabase();
                if (!b)
                {
                    Console.WriteLine("open database Error: ");
                }
                bool b1 = instance.databaseexist(databasename);
                if (!b1)
                {
                    instance.createdatabase(databasename);
                }
                instance.connection.ChangeDatabase(databasename);
                bool b2 = instance.tableeexist(databasename, tablebasename);
                if (!b2)
                {
                    instance.creatusersstable(databasename, tablebasename);
                }
            return instance;
        }
        ~MySQLOperation()
        {
            connection?.Close();

        }
        bool opendatabase()
        {
            try
            {
                connection.Open();
            }
            catch (MySqlException ex)
            {
                return false;

            }
            return true;
        }
        public void closedatabase()
        {
            connection?.Close();
        }
        private bool databaseexist(string databasename)
        {
            String command = String.Format(
                        "SELECT IF(EXISTS (SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '{0}'), 'Yes','No')  ", databasename
                        );
             MySqlDataReader rdr = null;
            try
            {
                MySqlCommand cmd = new MySqlCommand(command, connection);
                rdr = cmd.ExecuteReader();
                int column = rdr.FieldCount;
                if (rdr.Read())
                {
                    string o = (string)rdr[0];
                    if (o.Equals("Yes"))
                    {
                        return true;
                    }
                    else {
                        return false;
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Error: {0}", ex.ToString());

            }
            finally
            {
                if (rdr != null)
                {
                    rdr.Close();
                }

            }
            return false;
        }
        private bool tableeexist(string databasename, string tablename)
        {
            String command = String.Format(
                        "SELECT IF(EXISTS (SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE table_schema = '{0}' AND table_name = '{1}' ), 'Yes','No')  ", databasename, tablename
                        );
            MySqlDataReader rdr = null;
            try
            {
                MySqlCommand cmd = new MySqlCommand(command, connection);
                rdr = cmd.ExecuteReader();
                int column = rdr.FieldCount;
                if (rdr.Read())
                {
                    string o = (string)rdr[0];
                    if (o.Equals("Yes"))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Error: {0}", ex.ToString());

            }
            finally
            {

                if (rdr != null)
                {
                    rdr.Close();
                }

            }
            return false;
        }
        public bool find(String command)
        {
            lock (ReadLock)
            {
                MySqlDataReader rdr = null;
                try
                {
                    //string stm = "SELECT * FROM students";
                    //stm = "SELECT name FROM students";
                    MySqlCommand cmd = new MySqlCommand(command, connection);
                    rdr = cmd.ExecuteReader();
                    return rdr.Read();
                }
                catch (MySqlException ex)
                {
                    //Console.WriteLine("Error: {0}", ex.ToString());

                }
                finally
                {
                    if (rdr != null)
                    {
                        rdr.Close();
                    }
                }
                return false;
            }

        }
        public List<List<object>> get(String command)
        {
            lock (ReadLock)
            {
                Console.WriteLine("get");
                List<List<object>> value = new List<List<object>>();
                MySqlDataReader rdr = null;
                try
                {
                    //string stm = "SELECT * FROM students";
                    //stm = "SELECT name FROM students";
                    MySqlCommand cmd = new MySqlCommand(command, connection);
                    rdr = cmd.ExecuteReader();
                    int column = rdr.FieldCount;
                    while (rdr.Read())//maybe more than one row meet the requirements
                    {
                        List<object> item = new List<object>();
                        for (int i = 0; i < column; i++)
                        {
                            item.Add(rdr[i]);
                        }
                        value.Add(item);
                        // Console.WriteLine(rdr[0] + ":" + rdr[1] + ":" + rdr[2] + ":" + rdr[3]);
                    }

                }
                catch (MySqlException ex)
                {
                    Console.WriteLine("Error: {0}", ex.ToString());

                }
                finally
                {
                    if (rdr != null)
                    {
                        rdr.Close();
                    }
                }
                return value;
            }
        }

        public void add(String command, Dictionary<String, String> values)
        {
            lock (ReadLock)
            {
                try
                {
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = connection;
                    //cmd.CommandText = "INSERT INTO students(name,class) VALUES(@name,@class)";
                    cmd.CommandText = command;
                    cmd.Prepare();
                    foreach (KeyValuePair<String, String> pair in values)
                    {
                        cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                    }
                    //cmd.Parameters.AddWithValue("@name", "Gulbranssen1");
                    //cmd.Parameters.AddWithValue("@class", "senior1");
                    cmd.ExecuteNonQuery();

                }
                catch (MySqlException ex)
                {
                    Console.WriteLine("Error: {0}", ex.ToString());

                }
                finally
                {
                    //if (conn != null)
                    //{
                    //    conn.Close();
                    //}
                }
            }
        }

        public void modify(String command)
        {
            MySqlTransaction tr = null;

            lock (ReadLock)
            {
                try
                {
                    tr = connection.BeginTransaction();

                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = connection;
                    cmd.Transaction = tr;

                    cmd.CommandText = command;
                    cmd.ExecuteNonQuery();
                    //cmd.CommandText = "UPDATE students SET class='War and Peace' WHERE idstudents=5";
                    //cmd.ExecuteNonQuery();
                    //cmd.CommandText = "UPDATE students SET class='Anna Karenina' WHERE idstudents=2";
                    //cmd.ExecuteNonQuery();

                    tr.Commit();

                }
                catch (MySqlException ex)
                {
                    try
                    {
                        tr.Rollback();

                    }
                    catch (MySqlException ex1)
                    {
                        Console.WriteLine("Error: {0}", ex1.ToString());
                    }

                    Console.WriteLine("Error: {0}", ex.ToString());

                }
            }
        }

        public void delete(String command)
        {
            lock (ReadLock)
            {
                try
                {
                    //string des = "DELETE from students where name='Gulbranssen1'";
                    // des = "DELETE from students";//no condition ,this will delete all
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = connection;
                    cmd.CommandText = command;
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine("Error: {0}", ex.ToString());

                }
                finally
                {


                }
            }

        }
        public void creatfriendstable(String databasename, String tablename)
        {
            lock (ReadLock)
            {
                String command = String.Format(
                "CREATE TABLE `{0}`.`{1}`" +
                "(`idsometable` INT NOT NULL AUTO_INCREMENT," +
                "`name` VARCHAR(100) NULL," +
                "`number` VARCHAR(45) NULL," +
                "`moreinfor` JSON NULL," +
                "PRIMARY KEY (`idsometable`)) ", databasename, tablename
                    );
                try
                {
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = connection;
                    cmd.CommandText = command;
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine("Error: {0}", ex.ToString());

                }
                finally
                {


                }
            }
        }
        public void creatusersstable(String database, String tablename)
        {
            lock (ReadLock)
            {
                String command = String.Format(
                "CREATE TABLE `{0}`.`{1}`" +
                "(`idsometable` INT NOT NULL AUTO_INCREMENT," +
                 "`IsOnLine` TINYINT NULL," +
                 "`UserName` VARCHAR(45) NULL," +
                 "`UserUniqueID` VARCHAR(45) NULL," +
                 "`PassWord` VARCHAR(45) NULL," +
                 "`MapInfor` JSON NULL," +
                 "`FriendList_anothertable` VARCHAR(45) NULL," +
                "PRIMARY KEY (`idsometable`)) ", database, tablename
                    );
                try
                {
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = connection;
                    cmd.CommandText = command;
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }
                catch (MySqlException ex)
                //catch//if table already exist it will throw an exception
                {
                    Console.WriteLine("Error: {0}", ex.ToString());
                    //Console.WriteLine("some Error: maybe exist");

                }
                finally
                {


                }
            }
        }
        public void addcolumnv1(String columnname)
        {
            lock (ReadLock)
            {
                String command = String.Format(
                "ALTER TABLE `{0}`.`{1}` " +
                "ADD COLUMN `{2}` JSON NULL", databasename, tablebasename, columnname
                 );
                try
                {
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = connection;
                    cmd.CommandText = command;
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine("Error: {0}", ex.ToString());

                }
                finally
                {


                }
            }
        }
        public void droptable(String tablename)
        {
            lock (ReadLock)
            {
                String command = String.Format(
                "DROP TABLE `my_db`.`{0}`", tablename
                 );
                try
                {
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = connection;
                    cmd.CommandText = command;
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine("Error: {0}", ex.ToString());

                }
                finally
                {


                }
            }
        }
        public void createdatabase(String name)
        {
            lock (ReadLock)
            {
                String command = String.Format(
                "CREATE SCHEMA `{0}` ", name
                 );
                try
                {
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = connection;
                    cmd.CommandText = command;
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }
                catch (MySqlException ex)
              //  catch//if database already exist it will throw an exception
                {
                    Console.WriteLine("Error: {0}", ex.ToString());
                   // Console.WriteLine("some Error: maybe exist");

                }
                finally
                {


                }
            }
        }
    }
}
