using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Puniemu.Src.UserDataManager.DataClasses;
using Supabase;
using System.Collections;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text;
using System.Net.Http;

namespace Puniemu.Src.UserDataManager.Logic
{
    public static class UserDataManager
    {
        public class TableNotFoundException : Exception
        {
            public TableNotFoundException() : base() { }
        }

        public static Supabase.Client? SupabaseClient;

        //Check credentials and connect to the Firestore database.
        /*
           Run this on supabase:
           CREATE OR REPLACE FUNCTION exec_sql(query text)
           RETURNS void AS $$
           BEGIN
             EXECUTE query;
           END;
           $$ LANGUAGE plpgsql SECURITY DEFINER;
       */
        public static async Task Initialize()
        {
            try
            {
                SupabaseClient = new Supabase.Client(
                    DataManager.Logic.DataManager.SupabaseURL!,
                    DataManager.Logic.DataManager.SupabaseKey!,
                    new SupabaseOptions {
                        AutoRefreshToken = true
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Supabase Client: {ex.Message}");
                Environment.Exit(1);
            }
        
            string[] tablesSql = new[]
            {
                @"CREATE TABLE IF NOT EXISTS device (
                    udkey   TEXT PRIMARY KEY,
                    gdkeys  JSONB NULL DEFAULT '[]'
                );",
                @"CREATE TABLE IF NOT EXISTS account (
                    gdkey                   TEXT PRIMARY KEY,
                    character_id            TEXT NULL DEFAULT '',
                    user_id                 TEXT NULL DEFAULT '',
                    ywp_user_tables         JSONB NULL DEFAULT '{}',
                    last_lgn_time           TEXT NULL DEFAULT '',
                    start_date              BIGINT NULL DEFAULT 0,
                    opening_tutorial_flag   BOOLEAN NULL DEFAULT FALSE
                );"
            };
        
            using (var httpClient = new HttpClient())
            {
                var url = $"{DataManager.Logic.DataManager.SupabaseURL.TrimEnd('/')}/rest/v1/rpc/exec_sql";
                httpClient.DefaultRequestHeaders.Add("apikey", DataManager.Logic.DataManager.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {DataManager.Logic.DataManager.SupabaseKey}");
                foreach (var sql in tablesSql)
                {
                    try
                    {
                        var payload = JsonConvert.SerializeObject(new { query = sql.Trim() });
                        var content = new StringContent(payload, Encoding.UTF8, "application/json");
                        var response = await httpClient.PostAsync(url, content);
                        if (!response.IsSuccessStatusCode)
                        {
                            var errorBody = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"[SQL Error] {response.StatusCode}: {errorBody}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Request Exception] {ex.Message}");
                    }
                }
            }
        }

        // returns the udkey of the newly created device
        public static async Task<string> NewDeviceAsync()
        {
            var device = new Device() 
            { 
                UdKey = Guid.NewGuid().ToString(),
                Gdkeys = new()
            };
            var response = await SupabaseClient!.From<Device>().Insert(device);
            var newDevice = response.Models.First();
            return newDevice.UdKey;
        }
        // returns the udkey of the newly created device
        public static async Task<string> NewAccountAsync()
        {
            var acc = new Account()
            {
                Gdkey = Guid.NewGuid().ToString(),
                YwpUserTables = new(),
                LastLoginTime = "",
                CharacterId = ""
            };
            //response is saved to get the generated id
            var response = await SupabaseClient!.From<Account>().Insert(acc);
            var newAcc = response.Models.First();
            return newAcc.Gdkey;
        }
        //Gets user data from specific account
        public static async Task SetYwpUserAsync(string gdkey, string tableId, object data)
        {
            var response = await SupabaseClient!.From<Account>().Where(a => a.Gdkey == gdkey).Get();
            var account = response.Models.FirstOrDefault();
            account.YwpUserTables[tableId] = data;
            await account.Update<Account>();
        }

        public static async Task DeleteUser(string udkey, string gdkey)
        {
            await RemoveGdkeyFromUdkey(udkey, gdkey);
            await SupabaseClient.From<Account>().Where(a => a.Gdkey == gdkey).Delete();
        }

        private static async Task RemoveGdkeyFromUdkey(string udkey, string gdkey)
        {
            var response = await SupabaseClient.From<Device>().Where(d => d.UdKey == udkey).Get();
            var device = response.Models.FirstOrDefault();
            device.Gdkeys.Remove(gdkey);
        }
        //Sets user data for specific account
        public static async Task<T?> GetYwpUserAsync<T>(string gdkey, string tableId)
        {
            var response = await SupabaseClient!.From<Account>().Where(a => a.Gdkey == gdkey).Get();
            var account = response.Models.FirstOrDefault();
            var tbl = account.YwpUserTables[tableId];
            if (tbl == null)
                return default;
            JToken token = JToken.FromObject(tbl);
            return token.ToObject<T>();
        }
        public static async Task<Dictionary<string,object?>> GetEntireUserData(string gdkey)
        {
            var response = await SupabaseClient!.From<Account>().Where(a => a.Gdkey == gdkey).Get();
            var account = response.Models.FirstOrDefault();
            return account.YwpUserTables;
        }
        public static async Task<string> GetGdkeyFromCharacterId(string charId)
        {
            var response = await SupabaseClient!.From<Account>().Where(a => a.CharacterId == charId).Get();

            var account = response.Models.FirstOrDefault();
            return account?.Gdkey ?? string.Empty;
        }
        public static async Task<string> GetGdkeyFromUserId(string userId)
        {
            var response = await SupabaseClient!.From<Account>().Where(a => a.UserId == userId).Get();

            var account = response.Models.FirstOrDefault();
            return account?.Gdkey ?? string.Empty;
        }
        public static async Task<string> GetLastLoginTime(string gdkey)
        {
            var response = await SupabaseClient!.From<Account>().Where(a => a.Gdkey == gdkey).Get();

            var account = response.Models.FirstOrDefault();
            return account?.LastLoginTime;
        }
        public static async Task SetEntireUserData(string gdkey, Dictionary<string,object?> data)
        {
            var response = await SupabaseClient!.From<Account>().Where(a => a.Gdkey == gdkey).Get();
            var account = response.Models.FirstOrDefault();
            account.YwpUserTables = data;
            await account.Update<Account>();
        }
        //Gets all corresponding GDKeys from under a specified UDKey.
        public static async Task<List<string>> GetGdkeysFromUdkeyAsync(string udkey)
        {
            var response = await SupabaseClient.From<Device>().Where(d => d.UdKey == udkey).Get();
            var device = response.Models.FirstOrDefault();
            return device.Gdkeys;
        }
        //Add a gdkey association to a udkey
        public static async Task AddAccountToDevice(string udkey, string gdkey)
        {
            // get the correct device
            var response = await SupabaseClient.From<Device>().Where(d => d.UdKey == udkey).Get();
            var device = response.Models.FirstOrDefault();
            if(device.Gdkeys == null) device.Gdkeys = new List<string>();
            device.Gdkeys.Add(gdkey); 
            await device.Update<Device>();
        }
    }
}





