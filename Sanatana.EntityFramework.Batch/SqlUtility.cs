using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch
{
    public static class SqlUtility
    {
        /// <summary>
        /// Get range numbers for SQL rows numbering, where input Page number is expected to start from 0.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="numberStart"></param>
        /// <param name="numberEnd"></param>
        public static void SqlRowNumberZeroBased(int page, int pageSize, out int numberStart, out int numberEnd)
        {
            if (pageSize < 1)
            {
                throw new Exception("Number of items per page must be greater then 0.");
            }

            if (page < 0)
            {
                page = 0;
            }
            numberStart = page * pageSize + 1;
            numberEnd = numberStart + pageSize - 1;
        }

        /// <summary>
        /// Get number of rows to skip, where input Page number is expected to start from 0.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static int ToSkipNumberZeroBased(int page, int pageSize)
        {
            if (pageSize < 1)
            {
                throw new Exception("Number of items per page must be greater then 0.");
            }

            if (page < 0)
            {
                page = 0;
            }

            int skip = page * pageSize;
            return skip;
        }

        /// <summary>
        /// Get range numbers for SQL rows numbering, where input Page number is expected to start from 1.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="numberStart"></param>
        /// <param name="numberEnd"></param>
        public static void SqlRowNumberOneBased(int page, int pageSize, out int numberStart, out int numberEnd)
        {
            if (pageSize < 1)
            {
                throw new Exception("Number of items per page must be greater then 0.");
            }

            if (page < 1)
            {
                page = 1;
            }
            numberStart = (page - 1) * pageSize + 1;
            numberEnd = numberStart + pageSize - 1;
        }

        /// <summary>
        /// Get number of rows to skip, where input Page number is expected to start from 1.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static int ToSkipNumberOneBased(int page, int pageSize)
        {
            if (pageSize < 1)
            {
                throw new Exception("Number of items per page must be greater then 0.");
            }

            if (page < 1)
            {
                page = 1;
            }

            int skip = (page - 1) * pageSize;
            return skip;
        }

        /// <summary>
        /// Update C# Datetime value to fit in expected MSSQL small datetime format, 
        /// where 1900.1.1 12:00:00 is minimum value and 2079.6.6 11:59:59 is maximum value.
        /// </summary>
        /// <param name="datetime"></param>
        /// <returns></returns>
        public static DateTime ToSmallDateTime(DateTime datetime)
        {
            DateTime smallDateTimeMin = new DateTime(1900, 1, 1, 12, 0, 0);
            DateTime smallDateTimeMax = new DateTime(2079, 6, 6, 11, 59, 59);

            if (datetime < smallDateTimeMin)
            {
                datetime = smallDateTimeMin;
            }
            else if (datetime > smallDateTimeMax)
            {
                datetime = smallDateTimeMax;
            }
            return datetime;
        }

        /// <summary>
        /// Update C# TimeSpan to fit in expected MSSQL time format,
        /// where 0 is minimum value and 23 hours, 59 minute, 59 seconds, 999 is maximum value.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static TimeSpan ToSqlTime(TimeSpan time)
        {
            TimeSpan max = new TimeSpan(0, 23, 59, 59, 999).Add(TimeSpan.FromTicks(9999));

            if (time < TimeSpan.FromSeconds(0))
            {
                time = TimeSpan.FromSeconds(0);
            }
            else if (time > max)
            {
                time = max;
            }
            return time;
        }
    }
}
