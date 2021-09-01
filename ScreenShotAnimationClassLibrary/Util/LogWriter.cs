using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenShotAnimation.Util
{
    public class LogWriter
    {
        public static async Task ErrorAsync(Exception ex)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {ex.Message}");
                sb.AppendLine(ex.ToString());

                await Task.Run(() => File.AppendAllText(@"log.txt", sb.ToString()));
            }
            catch
            {
                //エラーは無視
            }
        }

        public static async Task InformationAsync(string message)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {message}");

                await Task.Run(() => File.AppendAllText(@"log.txt", sb.ToString()));
            }
            catch
            {
                //エラーは無視
            }
        }
    }
}
