using Newtonsoft.Json;
using Puniemu.Src.Server.GameServer.Requests.CreateUser.DataClasses;
using Puniemu.Src.Server.GameServer.DataClasses;
using System.Text;
using System.Buffers;
using Puniemu.Src.UserDataManager.Logic;
using Puniemu.Src.DataManager.Logic;
using Puniemu.Src.UserDataManager.DataClasses;

namespace Puniemu.Src.Server.GameServer.Requests.CreateUser.Logic
{
    public class CreateUserHandler
    {
        private static Account Acc { get; set; }
        public static async Task HandleAsync(HttpContext ctx)
        {
            ctx.Request.EnableBuffering();
            var readResult = await ctx.Request.BodyReader.ReadAsync();
            var encRequest = Encoding.UTF8.GetString(readResult.Buffer.ToArray());
            ctx.Request.BodyReader.AdvanceTo(readResult.Buffer.End);
            var requestJsonString = NHNCrypt.Logic.NHNCrypt.DecryptRequest(encRequest);
            var deserialized = JsonConvert.DeserializeObject<CreateUserRequest>(requestJsonString!);
            var dbres = await UserDataManager.Logic.UserDataManager.SupabaseClient.From<Account>().Where(x => x.Gdkey == deserialized.Level5UserID).Get();
            Acc = dbres.Model;
            ctx.Response.ContentType = "application/json";
            var generatedUserData = new YwpUserData((PlayerIcon)deserialized.IconID, (PlayerTitle)deserialized.IconID, deserialized.Level5UserID, deserialized.PlayerName);
            Acc.StartDate = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            Acc.CharacterId = generatedUserData.CharacterID;
            Acc.UserId = generatedUserData.UserID;
            await Acc.Update<Account>();
            try
            {
                await RegisterDefaultTables(deserialized, generatedUserData);
            }
            catch (Exception e)
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.WriteAsync($"Error: {e.Message}"); 
                return;
            }
            var createUserResponse = new CreateUserResponse(DataManager.Logic.DataManager.GameDataManager.GamedataCache["ywp_user_tutorial_list_def"], generatedUserData);
            var marshalledResponse = JsonConvert.SerializeObject(createUserResponse);
            var encryptedResponse = NHNCrypt.Logic.NHNCrypt.EncryptResponse(marshalledResponse);
            await ctx.Response.WriteAsync(encryptedResponse);
            
        }

        private static async Task RegisterDefaultTables(CreateUserRequest deserialized, YwpUserData generatedUserData)
        {
            Dictionary<string, object?> tables = new Dictionary<string, object?>();
            
            tables["opening_tutorial_flg"] = false;
        
            foreach (var userTable in Consts.LOGIN_TABLES.Where(x => x.Contains("ywp_user") && x != "ywp_user_data"))
            {
                if (DataManager.Logic.DataManager.GameDataManager!.GamedataCache.TryGetValue(userTable + "_def", out var data))
                {
                    object? deserializedDefaultUserTable = null;
                    try
                    {
                        deserializedDefaultUserTable = JsonConvert.DeserializeObject<object>(data);
                    }
                    catch
                    {
                        deserializedDefaultUserTable = data;
                    }
        
                    // Utilisation de l'indexeur au lieu de .Add()
                    tables[userTable] = deserializedDefaultUserTable;
                }
                else
                {
                    // Message d'erreur explicite pour le débug
                    throw new Exception($"Missing default data for table: {userTable}_def");
                }
            }
        
            // Set ywpuser data (écrase si déjà présent dans LOGIN_TABLES)
            tables["ywp_user_data"] = generatedUserData;
            tables["ywp_user_gacha_stamp"] = "";
        
            List<YokaiCollectEntry> yokaiCollect = new();
            tables["ywp_user_youkai_collect"] = yokaiCollect;
        
            List<YokaiIntroEntry> yokaiIntro = new();
            tables["ywp_user_youkai_intro"] = yokaiIntro;
        
            List<object> empty = new();
            tables["ywp_user_goku_youkai_intro_release"] = empty;
            tables["ywp_user_goku_story"] = empty;
            tables["ywp_user_friend_request_recv"] = empty;
            tables["ywp_user_friend"] = empty;
        
            var val = new FriendRankEntry
            {
                IconId = generatedUserData.IconID,
                PlayerName = generatedUserData.PlayerName,
                TitleId = generatedUserData.CharacterTitleID,
                GetStar = 0,
                UserId = generatedUserData.UserID,
                DicCnt = 0,
                Score = 0,
                YoukaiId = generatedUserData.YoukaiId,
                GetStarModiDt = null,
                HitodamaSendFlg = 1,
                OnedariSendFlg = 1,
                Rank = 1,
                Self = 1,
            };
        
            tables["ywp_user_present_box_list"] = empty;
            tables["ywp_user_score_attack_reward"] = empty;
            tables["ywp_user_league_rank"] = null;
        
            List<FriendRankEntry> val2 = new() { val };
            tables["ywp_user_friend_star_rank"] = val2;
            tables["ywp_user_friend_rank"] = val2;
            tables["ywp_user_friend_dictionary_rank"] = val2;
            tables["ywp_user_self_rank"] = new SelfRank(generatedUserData);
        
            // Set start date
            tables["login_stamp"] = "0|0|0";
        
            await UserDataManager.Logic.UserDataManager.SetEntireUserData(deserialized.Level5UserID, tables!);
        }
    }
}
