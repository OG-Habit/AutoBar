﻿using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoBarBar
{
    public static class Constants
    {
        public static readonly string SERVER = "sql6.freemysqlhosting.net";
        public static readonly string USERID = "sql6494729";
        public static readonly string PASSWORD = "gEB2fyY5T4";
        public static readonly string DATABASE = "sql6494729";
        public static readonly string CONNECTION_STRING = new MySqlConnectionStringBuilder
            {
                Server = SERVER,
                UserID = USERID,
                Password = PASSWORD,
                Database = DATABASE
            }.ConnectionString;

        public static readonly string KEY_DB = "db";
        public static readonly string KEY_UI = "ui";
        public static readonly string KEY_ISLOGGED = "isLogged";

        public static readonly string PARAM_CUSTOMER_IDS = "customerIDs";
        public static readonly string PARAM_USER = "user";
        public static readonly string PARAM_NEW_TAB = "newTab";

        public static readonly int IMG_QR_LENGTH = 500;
    }
}
