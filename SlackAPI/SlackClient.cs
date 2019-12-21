using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using SlackAPI.RPCMessages;
using System.Threading.Tasks;

namespace SlackAPI
{
    /// <summary>
    /// SlackClient is intended to solely handle RPC (HTTP-based) functionality. Does not handle WebSocket connectivity.
    ///
    /// For WebSocket connectivity, refer to <see cref="SlackAPI.SlackSocketClient"/>
    /// </summary>
    public class SlackClient : SlackClientBase
    {
        private readonly string APIToken;

        public Self MySelf;
        public User MyData;
        public Team MyTeam;

        public List<string> starredChannels;

        public List<User> Users;
        public List<Bot> Bots;
        public List<Channel> Channels;
        public List<Channel> Groups;
        public List<DirectMessageConversation> DirectMessages;

        public Dictionary<string, User> UserLookup;
        public Dictionary<string, Channel> ChannelLookup;
        public Dictionary<string, Channel> GroupLookup;
        public Dictionary<string, DirectMessageConversation> DirectMessageLookup;
        public Dictionary<string, Conversation> ConversationLookup;

        public SlackClient(string token)
        {
            APIToken = token;
        }

        public SlackClient(string token, IWebProxy proxySettings)
            : base(proxySettings)
        {
            APIToken = token;
        }

        public virtual void Connect(Action<LoginResponse> onConnected = null, Action onSocketConnected = null)
        {
            EmitLogin((loginDetails) =>
            {
                if (loginDetails.ok)
                    Connected(loginDetails);

                if (onConnected != null)
                    onConnected(loginDetails);
            });
        }

        protected virtual void Connected(LoginResponse loginDetails)
        {
            MySelf = loginDetails.self;
            MyData = loginDetails.users.First((c) => c.id == MySelf.id);
            MyTeam = loginDetails.team;

            Users = new List<User>(loginDetails.users.Where((c) => !c.deleted));
            Bots = new List<Bot>(loginDetails.bots.Where((c) => !c.deleted));
            Channels = new List<Channel>(loginDetails.channels);
            Groups = new List<Channel>(loginDetails.groups);
            DirectMessages = new List<DirectMessageConversation>(loginDetails.ims.Where((c) => Users.Exists((a) => a.id == c.user) && c.id != MySelf.id));
            starredChannels =
                    Groups.Where((c) => c.is_starred).Select((c) => c.id)
                .Union(
                    DirectMessages.Where((c) => c.is_starred).Select((c) => c.user)
                ).Union(
                    Channels.Where((c) => c.is_starred).Select((c) => c.id)
                ).ToList();

            UserLookup = new Dictionary<string, User>();
            foreach (User u in Users) UserLookup.Add(u.id, u);

            ChannelLookup = new Dictionary<string, Channel>();
            ConversationLookup = new Dictionary<string, Conversation>();
            foreach (Channel c in Channels)
            {
                ChannelLookup.Add(c.id, c);
                ConversationLookup.Add(c.id, c);
            }

            GroupLookup = new Dictionary<string, Channel>();
            foreach (Channel g in Groups)
            {
                GroupLookup.Add(g.id, g);
                ConversationLookup.Add(g.id, g);
            }

            DirectMessageLookup = new Dictionary<string, DirectMessageConversation>();
            foreach (DirectMessageConversation im in DirectMessages)
            {
                DirectMessageLookup.Add(im.id, im);
                ConversationLookup.Add(im.id, im);
            }
        }

        public Task DeleteReactionAsync(string v, string channel, string message_ts)
        {
            throw new NotImplementedException();
        }

        public void APIRequestWithToken<K>(Action<K> callback, params Tuple<string, string>[] getParameters)
            where K : Response
        {
            Tuple<string, string>[] tokenArray = new Tuple<string, string>[]{
                new Tuple<string,string>("token", APIToken)
            };

            if (getParameters != null && getParameters.Length > 0)
                tokenArray = tokenArray.Concat(getParameters).ToArray();

            APIRequest(callback, tokenArray, new Tuple<string, string>[0]);
        }

        public Task<K> APIRequestWithTokenAsync<K>(params Tuple<string, string>[] getParameters)
    where K : Response
        {
            Tuple<string, string>[] tokenArray = new Tuple<string, string>[]{
                new Tuple<string,string>("token", APIToken)
            };

            if (getParameters != null && getParameters.Length > 0)
                tokenArray = tokenArray.Concat(getParameters).ToArray();

            return APIRequestAsync<K>(tokenArray, new Tuple<string, string>[0]);
        }

        public void TestAuth(Action<AuthTestResponse> callback)
        {
            APIRequestWithToken(callback);
        }
        public Task<AuthTestResponse> TestAuthAsync()
        {
            return APIRequestWithTokenAsync<AuthTestResponse>();
        }

        public void GetUserList(Action<UserListResponse> callback)
        {
            APIRequestWithToken(callback);
        }

        public Task<UserListResponse> GetUserListAsync(Action<UserListResponse> callback)
        {
            return APIRequestWithTokenAsync<UserListResponse>();
        }

        public void ChannelsCreate(Action<ChannelCreateResponse> callback, string name)
        {
            APIRequestWithToken(callback, new Tuple<string, string>("name", name));
        }

        public Task<ChannelCreateResponse> ChannelsCreateAsync(Action<ChannelCreateResponse> callback, string name)
        {
            return APIRequestWithTokenAsync<ChannelCreateResponse>(new Tuple<string, string>("name", name));
        }

        public void ChannelsInvite(Action<ChannelInviteResponse> callback, string userId, string channelId)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            parameters.Add(new Tuple<string, string>("channel", channelId));
            parameters.Add(new Tuple<string, string>("user", userId));

            APIRequestWithToken(callback, parameters.ToArray());
        }

        public Task<ChannelInviteResponse> ChannelsInviteAsync(string userId, string channelId)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            parameters.Add(new Tuple<string, string>("channel", channelId));
            parameters.Add(new Tuple<string, string>("user", userId));

            return APIRequestWithTokenAsync<ChannelInviteResponse>(parameters.ToArray());
        }

        public void GetChannelList(Action<ChannelListResponse> callback, bool ExcludeArchived = true)
        {
            APIRequestWithToken(callback, new Tuple<string, string>("exclude_archived", ExcludeArchived ? "1" : "0"));
        }

        public Task<ChannelListResponse> GetChannelListAsync(bool ExcludeArchived = true)
        {
            return APIRequestWithTokenAsync<ChannelListResponse>(new Tuple<string, string>("exclude_archived", ExcludeArchived ? "1" : "0"));
        }

        public void GetGroupsList(Action<GroupListResponse> callback, bool ExcludeArchived = true)
        {
            APIRequestWithToken(callback, new Tuple<string, string>("exclude_archived", ExcludeArchived ? "1" : "0"));
        }
        public Task<GroupListResponse> GetGroupsListAsync(Action<GroupListResponse> callback, bool ExcludeArchived = true)
        {
            return APIRequestWithTokenAsync<GroupListResponse>(new Tuple<string, string>("exclude_archived", ExcludeArchived ? "1" : "0"));
        }

        public void GetDirectMessageList(Action<DirectMessageConversationListResponse> callback)
        {
            APIRequestWithToken(callback);
        }
        public Task<DirectMessageConversationListResponse> GetDirectMessageListAsync()
        {
            return APIRequestWithTokenAsync<DirectMessageConversationListResponse>();
        }

        public void GetFiles(Action<FileListResponse> callback, string userId = null, DateTime? from = null, DateTime? to = null, int? count = null, int? page = null, FileTypes types = FileTypes.all, string channel = null)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            if (!string.IsNullOrEmpty(userId))
                parameters.Add(new Tuple<string, string>("user", userId));

            if (from.HasValue)
                parameters.Add(new Tuple<string, string>("ts_from", from.Value.ToProperTimeStamp()));

            if (to.HasValue)
                parameters.Add(new Tuple<string, string>("ts_to", to.Value.ToProperTimeStamp()));

            if (!types.HasFlag(FileTypes.all))
            {
                FileTypes[] values = (FileTypes[])Enum.GetValues(typeof(FileTypes));

                StringBuilder building = new StringBuilder();
                bool first = true;
                for (int i = 0; i < values.Length; ++i)
                {
                    if (types.HasFlag(values[i]))
                    {
                        if (!first) building.Append(",");

                        building.Append(values[i].ToString());

                        first = false;
                    }
                }

                if (building.Length > 0)
                    parameters.Add(new Tuple<string, string>("types", building.ToString()));
            }

            if (count.HasValue)
                parameters.Add(new Tuple<string, string>("count", count.Value.ToString()));

            if (page.HasValue)
                parameters.Add(new Tuple<string, string>("page", page.Value.ToString()));

            if (!string.IsNullOrEmpty(channel))
                parameters.Add(new Tuple<string, string>("channel", channel));

            APIRequestWithToken(callback, parameters.ToArray());
        }

        public Task<FileListResponse> GetFiles(string userId = null, DateTime? from = null, DateTime? to = null, int? count = null, int? page = null, FileTypes types = FileTypes.all, string channel = null)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            if (!string.IsNullOrEmpty(userId))
                parameters.Add(new Tuple<string, string>("user", userId));

            if (from.HasValue)
                parameters.Add(new Tuple<string, string>("ts_from", from.Value.ToProperTimeStamp()));

            if (to.HasValue)
                parameters.Add(new Tuple<string, string>("ts_to", to.Value.ToProperTimeStamp()));

            if (!types.HasFlag(FileTypes.all))
            {
                FileTypes[] values = (FileTypes[])Enum.GetValues(typeof(FileTypes));

                StringBuilder building = new StringBuilder();
                bool first = true;
                for (int i = 0; i < values.Length; ++i)
                {
                    if (types.HasFlag(values[i]))
                    {
                        if (!first) building.Append(",");

                        building.Append(values[i].ToString());

                        first = false;
                    }
                }

                if (building.Length > 0)
                    parameters.Add(new Tuple<string, string>("types", building.ToString()));
            }

            if (count.HasValue)
                parameters.Add(new Tuple<string, string>("count", count.Value.ToString()));

            if (page.HasValue)
                parameters.Add(new Tuple<string, string>("page", page.Value.ToString()));

            if (!string.IsNullOrEmpty(channel))
                parameters.Add(new Tuple<string, string>("channel", channel));

            return APIRequestWithTokenAsync<FileListResponse>(parameters.ToArray());
        }

        void GetHistory<K>(Action<K> historyCallback, string channel, DateTime? latest = null, DateTime? oldest = null, int? count = null, bool? unreads = false)

            where K : MessageHistory
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();
            parameters.Add(new Tuple<string, string>("channel", channel));

            if (latest.HasValue)
                parameters.Add(new Tuple<string, string>("latest", latest.Value.ToProperTimeStamp()));
            if (oldest.HasValue)
                parameters.Add(new Tuple<string, string>("oldest", oldest.Value.ToProperTimeStamp()));

            if(count.HasValue)
                parameters.Add(new Tuple<string,string>("count", count.Value.ToString()));
            if (unreads.HasValue)
                parameters.Add(new Tuple<string, string>("unreads", unreads.Value ? "1" : "0"));

            APIRequestWithToken(historyCallback, parameters.ToArray());
        }

        Task<K> GetHistoryAsync<K>(string channel, DateTime? latest = null, DateTime? oldest = null, int? count = null, bool? unreads = false)

            where K : MessageHistory
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();
            parameters.Add(new Tuple<string, string>("channel", channel));

            if (latest.HasValue)
                parameters.Add(new Tuple<string, string>("latest", latest.Value.ToProperTimeStamp()));
            if (oldest.HasValue)
                parameters.Add(new Tuple<string, string>("oldest", oldest.Value.ToProperTimeStamp()));

            if (count.HasValue)
                parameters.Add(new Tuple<string, string>("count", count.Value.ToString()));
            if (unreads.HasValue)
                parameters.Add(new Tuple<string, string>("unreads", unreads.Value ? "1" : "0"));

            return APIRequestWithTokenAsync<K>(parameters.ToArray());
        }

        public void GetChannelHistory(Action<ChannelMessageHistory> callback, Channel channelInfo, DateTime? latest = null, DateTime? oldest = null, int? count = null, bool? unreads = false)
        {
            GetHistory(callback, channelInfo.id, latest, oldest, count, unreads);
        }
        public Task<ChannelMessageHistory> GetChannelHistoryAsync(Channel channelInfo, DateTime? latest = null, DateTime? oldest = null, int? count = null, bool? unreads = false)
        {
            return GetHistoryAsync<ChannelMessageHistory>(channelInfo.id, latest, oldest, count, unreads);
        }

        public void GetDirectMessageHistory(Action<MessageHistory> callback, DirectMessageConversation conversationInfo, DateTime? latest = null, DateTime? oldest = null, int? count = null, bool? unreads = false)
        {
            GetHistory(callback, conversationInfo.id, latest, oldest, count, unreads);
        }
        public Task<MessageHistory> GetDirectMessageHistoryAsync(DirectMessageConversation conversationInfo, DateTime? latest = null, DateTime? oldest = null, int? count = null, bool? unreads = false)
        {
            return GetHistoryAsync<MessageHistory>(conversationInfo.id, latest, oldest, count, unreads);
        }

        public void GetGroupHistory(Action<GroupMessageHistory> callback, Channel groupInfo, DateTime? latest = null, DateTime? oldest = null, int? count = null, bool? unreads = false)
        {
            GetHistory(callback, groupInfo.id, latest, oldest, count, unreads);
        }

        public Task<GroupMessageHistory> GetGroupHistoryAsync(Channel groupInfo, DateTime? latest = null, DateTime? oldest = null, int? count = null, bool? unreads = false)
        {
            return GetHistoryAsync<GroupMessageHistory>(groupInfo.id, latest, oldest, count, unreads);
        }
        public void MarkChannel(Action<MarkResponse> callback, string channelId, DateTime ts)
        {
            APIRequestWithToken(callback,
                new Tuple<string, string>("channel", channelId),
                new Tuple<string, string>("ts", ts.ToProperTimeStamp())
            );
        }

        public void GetFileInfo(Action<FileInfoResponse> callback, string fileId, int? page = null, int? count = null)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            parameters.Add(new Tuple<string, string>("file", fileId));

            if (count.HasValue)
                parameters.Add(new Tuple<string, string>("count", count.Value.ToString()));

            if (page.HasValue)
                parameters.Add(new Tuple<string, string>("page", page.Value.ToString()));

            APIRequestWithToken(callback, parameters.ToArray());
        }
        #region Groups
        public void GroupsArchive(Action<GroupArchiveResponse> callback, string channelId)
        {
            APIRequestWithToken(callback, new Tuple<string, string>("channel", channelId));
        }

        public void GroupsClose(Action<GroupCloseResponse> callback, string channelId)
        {
            APIRequestWithToken(callback, new Tuple<string, string>("channel", channelId));
        }

        public void GroupsCreate(Action<GroupCreateResponse> callback, string name)
        {
            APIRequestWithToken(callback, new Tuple<string, string>("name", name));
        }

        public void GroupsCreateChild(Action<GroupCreateChildResponse> callback, string channelId)
        {
            APIRequestWithToken(callback, new Tuple<string, string>("channel", channelId));
        }

        public void GroupsInvite(Action<GroupInviteResponse> callback, string userId, string channelId)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            parameters.Add(new Tuple<string, string>("channel", channelId));
            parameters.Add(new Tuple<string, string>("user", userId));

            APIRequestWithToken(callback, parameters.ToArray());
        }

        public void GroupsKick(Action<GroupKickResponse> callback, string userId, string channelId)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            parameters.Add(new Tuple<string, string>("channel", channelId));
            parameters.Add(new Tuple<string, string>("user", userId));

            APIRequestWithToken(callback, parameters.ToArray());
        }

        public void GroupsLeave(Action<GroupLeaveResponse> callback, string channelId)
        {
            APIRequestWithToken(callback, new Tuple<string, string>("channel", channelId));
        }

        public void GroupsMark(Action<GroupMarkResponse> callback, string channelId, DateTime ts)
        {
            APIRequestWithToken(callback, new Tuple<string, string>("channel", channelId), new Tuple<string, string>("ts", ts.ToProperTimeStamp()));
        }

        public void GroupsOpen(Action<GroupOpenResponse> callback, string channelId)
        {
            APIRequestWithToken(callback, new Tuple<string, string>("channel", channelId));
        }

        public void GroupsRename(Action<GroupRenameResponse> callback, string channelId, string name)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            parameters.Add(new Tuple<string, string>("channel", channelId));
            parameters.Add(new Tuple<string, string>("name", name));

            APIRequestWithToken(callback, parameters.ToArray());
        }

        public void GroupsSetPurpose(Action<GroupSetPurposeResponse> callback, string channelId, string purpose)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            parameters.Add(new Tuple<string, string>("channel", channelId));
            parameters.Add(new Tuple<string, string>("purpose", purpose));

            APIRequestWithToken(callback, parameters.ToArray());
        }

        public void GroupsSetTopic(Action<GroupSetPurposeResponse> callback, string channelId, string topic)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            parameters.Add(new Tuple<string, string>("channel", channelId));
            parameters.Add(new Tuple<string, string>("topic", topic));

            APIRequestWithToken(callback, parameters.ToArray());
        }

        public void GroupsUnarchive(Action<GroupUnarchiveResponse> callback, string channelId)
        {
            APIRequestWithToken(callback, new Tuple<string, string>("channel", channelId));
        }

        #endregion

        public void SearchAll(Action<SearchResponseAll> callback, string query, string sorting = null, SearchSortDirection? direction = null, bool enableHighlights = false, int? count = null, int? page = null)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();
            parameters.Add(new Tuple<string, string>("query", query));

            if (sorting != null)
                parameters.Add(new Tuple<string, string>("sort", sorting));

            if (direction.HasValue)
                parameters.Add(new Tuple<string, string>("sort_dir", direction.Value.ToString()));

            if (enableHighlights)
                parameters.Add(new Tuple<string, string>("highlight", "1"));

            if (count.HasValue)
                parameters.Add(new Tuple<string, string>("count", count.Value.ToString()));

            if (page.HasValue)
                parameters.Add(new Tuple<string, string>("page", page.Value.ToString()));

            APIRequestWithToken(callback, parameters.ToArray());
        }

        public void SearchMessages(Action<SearchResponseMessages> callback, string query, string sorting = null, SearchSortDirection? direction = null, bool enableHighlights = false, int? count = null, int? page = null)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();
            parameters.Add(new Tuple<string, string>("query", query));

            if (sorting != null)
                parameters.Add(new Tuple<string, string>("sort", sorting));

            if (direction.HasValue)
                parameters.Add(new Tuple<string, string>("sort_dir", direction.Value.ToString()));

            if (enableHighlights)
                parameters.Add(new Tuple<string, string>("highlight", "1"));

            if (count.HasValue)
                parameters.Add(new Tuple<string, string>("count", count.Value.ToString()));

            if (page.HasValue)
                parameters.Add(new Tuple<string, string>("page", page.Value.ToString()));

            APIRequestWithToken(callback, parameters.ToArray());
        }

        public Task<SearchResponseMessages> SearchMessagesAsync(string query, string sorting = null, SearchSortDirection? direction = null, bool enableHighlights = false, int? count = null, int? page = null)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();
            parameters.Add(new Tuple<string, string>("query", query));

            if (sorting != null)
                parameters.Add(new Tuple<string, string>("sort", sorting));

            if (direction.HasValue)
                parameters.Add(new Tuple<string, string>("sort_dir", direction.Value.ToString()));

            if (enableHighlights)
                parameters.Add(new Tuple<string, string>("highlight", "1"));

            if (count.HasValue)
                parameters.Add(new Tuple<string, string>("count", count.Value.ToString()));

            if (page.HasValue)
                parameters.Add(new Tuple<string, string>("page", page.Value.ToString()));

            return APIRequestWithTokenAsync<SearchResponseMessages>(parameters.ToArray());
        }

        public void SearchFiles(Action<SearchResponseFiles> callback, string query, string sorting = null, SearchSortDirection? direction = null, bool enableHighlights = false, int? count = null, int? page = null)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();
            parameters.Add(new Tuple<string, string>("query", query));

            if (sorting != null)
                parameters.Add(new Tuple<string, string>("sort", sorting));

            if (direction.HasValue)
                parameters.Add(new Tuple<string, string>("sort_dir", direction.Value.ToString()));

            if (enableHighlights)
                parameters.Add(new Tuple<string, string>("highlight", "1"));

            if (count.HasValue)
                parameters.Add(new Tuple<string, string>("count", count.Value.ToString()));

            if (page.HasValue)
                parameters.Add(new Tuple<string, string>("page", page.Value.ToString()));

            APIRequestWithToken(callback, parameters.ToArray());
        }
        public Task<SearchResponseFiles> SearchFilesAsync(string query, string sorting = null, SearchSortDirection? direction = null, bool enableHighlights = false, int? count = null, int? page = null)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();
            parameters.Add(new Tuple<string, string>("query", query));

            if (sorting != null)
                parameters.Add(new Tuple<string, string>("sort", sorting));

            if (direction.HasValue)
                parameters.Add(new Tuple<string, string>("sort_dir", direction.Value.ToString()));

            if (enableHighlights)
                parameters.Add(new Tuple<string, string>("highlight", "1"));

            if (count.HasValue)
                parameters.Add(new Tuple<string, string>("count", count.Value.ToString()));

            if (page.HasValue)
                parameters.Add(new Tuple<string, string>("page", page.Value.ToString()));

            return APIRequestWithTokenAsync<SearchResponseFiles>(parameters.ToArray());
        }

        public void GetStars(Action<StarListResponse> callback, string userId = null, int? count = null, int? page = null)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            if (!string.IsNullOrEmpty(userId))
                parameters.Add(new Tuple<string, string>("user", userId));

            if (count.HasValue)
                parameters.Add(new Tuple<string, string>("count", count.Value.ToString()));

            if (page.HasValue)
                parameters.Add(new Tuple<string, string>("page", page.Value.ToString()));

            APIRequestWithToken(callback, parameters.ToArray());
        }

        public Task<StarListResponse> GetStarsAsync(string userId = null, int? count = null, int? page = null)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            if (!string.IsNullOrEmpty(userId))
                parameters.Add(new Tuple<string, string>("user", userId));

            if (count.HasValue)
                parameters.Add(new Tuple<string, string>("count", count.Value.ToString()));

            if (page.HasValue)
                parameters.Add(new Tuple<string, string>("page", page.Value.ToString()));

            return APIRequestWithTokenAsync<StarListResponse>(parameters.ToArray());
        }

        public void DeleteMessage(Action<DeletedResponse> callback, string channelId, DateTime ts)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>()
            {
                new Tuple<string,string>("ts", ts.ToProperTimeStamp()),
                new Tuple<string,string>("channel", channelId)
            };

            APIRequestWithToken(callback, parameters.ToArray());
        }
        public Task<DeletedResponse> DeleteMessageAsync(string channelId, DateTime ts)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>()
            {
                new Tuple<string,string>("ts", ts.ToProperTimeStamp()),
                new Tuple<string,string>("channel", channelId)
            };

            return APIRequestWithTokenAsync<DeletedResponse>(parameters.ToArray());
        }

        public void EmitPresence(Action<PresenceResponse> callback, Presence status)
        {
            APIRequestWithToken(callback, new Tuple<string, string>("presence", status.ToString()));
        }
        public Task<PresenceResponse> EmitPresenceAsync(Presence status)
        {
            return APIRequestWithTokenAsync<PresenceResponse>(new Tuple<string, string>("presence", status.ToString()));
        }

        public void GetPreferences(Action<UserPreferencesResponse> callback)
        {
            APIRequestWithToken(callback);
        }

        public Task<UserPreferencesResponse> GetPreferences()
        {
            return APIRequestWithTokenAsync<UserPreferencesResponse>();
        }

        #region Users

        public void GetCounts(Action<UserCountsResponse> callback)
        {
            APIRequestWithToken(callback);
        }

        public void GetPresence(Action<UserGetPresenceResponse> callback, string user)
        {
            APIRequestWithToken(callback, new Tuple<string, string>("user", user));
        }

        public void GetInfo(Action<UserInfoResponse> callback, string user)
        {
            APIRequestWithToken(callback, new Tuple<string, string>("user", user));
        }

        #endregion

        public void EmitLogin(Action<LoginResponse> callback, string agent = "Inumedia.SlackAPI")
        {
            APIRequestWithToken(callback, new Tuple<string, string>("agent", agent));
        }

        public void Update(
            Action<UpdateResponse> callback,
            string ts,
            string channelId,
            string text,
            string botName = null,
            string parse = null,
            bool linkNames = false,
            IBlock[] blocks = null,
            Attachment[] attachments = null,
            bool as_user = false)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            parameters.Add(new Tuple<string, string>("ts", ts));
            parameters.Add(new Tuple<string, string>("channel", channelId));
            parameters.Add(new Tuple<string, string>("text", text));

            if (!string.IsNullOrEmpty(botName))
                parameters.Add(new Tuple<string, string>("username", botName));

            if (!string.IsNullOrEmpty(parse))
                parameters.Add(new Tuple<string, string>("parse", parse));

            if (linkNames)
                parameters.Add(new Tuple<string, string>("link_names", "1"));

            if (blocks != null && blocks.Length > 0)
                parameters.Add(new Tuple<string, string>("blocks",
                   JsonConvert.SerializeObject(blocks, new JsonSerializerSettings()
                   {
                       NullValueHandling = NullValueHandling.Ignore
                   })));

            if (attachments != null && attachments.Length > 0)
                parameters.Add(new Tuple<string, string>("attachments",
                   JsonConvert.SerializeObject(attachments, new JsonSerializerSettings()
                   {
                       NullValueHandling = NullValueHandling.Ignore
                   })));


            parameters.Add(new Tuple<string, string>("as_user", as_user.ToString()));

            APIRequestWithToken(callback, parameters.ToArray());
        }

        public Task<UpdateResponse> UpdateAsync(
            string ts,
            string channelId,
            string text,
            string botName = null,
            string parse = null,
            bool linkNames = false,
            IBlock[] blocks = null,
            Attachment[] attachments = null,
            bool as_user = false)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            parameters.Add(new Tuple<string, string>("ts", ts));
            parameters.Add(new Tuple<string, string>("channel", channelId));
            parameters.Add(new Tuple<string, string>("text", text));

            if (!string.IsNullOrEmpty(botName))
                parameters.Add(new Tuple<string, string>("username", botName));

            if (!string.IsNullOrEmpty(parse))
                parameters.Add(new Tuple<string, string>("parse", parse));

            if (linkNames)
                parameters.Add(new Tuple<string, string>("link_names", "1"));

            if (blocks != null && blocks.Length > 0)
                parameters.Add(new Tuple<string, string>("blocks",
                   JsonConvert.SerializeObject(blocks, new JsonSerializerSettings()
                   {
                       NullValueHandling = NullValueHandling.Ignore
                   })));

            if (attachments != null && attachments.Length > 0)
                parameters.Add(new Tuple<string, string>("attachments",
                   JsonConvert.SerializeObject(attachments, new JsonSerializerSettings()
                   {
                       NullValueHandling = NullValueHandling.Ignore
                   })));


            parameters.Add(new Tuple<string, string>("as_user", as_user.ToString()));

            return APIRequestWithTokenAsync< UpdateResponse>(parameters.ToArray());
        }

        public void JoinDirectMessageChannel(Action<JoinDirectMessageChannelResponse> callback, string user)
        {
            var param = new Tuple<string, string>("user", user);
            APIRequestWithToken(callback, param);
        }

        public void PostMessage(
            Action<PostMessageResponse> callback,
            string channelId,
            string text,
            string botName = null,
            string parse = null,
            bool linkNames = false,
            IBlock[] blocks = null,
            Attachment[] attachments = null,
            bool unfurl_links = false,
            string icon_url = null,
            string icon_emoji = null,
            bool? as_user = null,
              string thread_ts = null)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            parameters.Add(new Tuple<string, string>("channel", channelId));
            parameters.Add(new Tuple<string, string>("text", text));

            if (!string.IsNullOrEmpty(botName))
                parameters.Add(new Tuple<string, string>("username", botName));

            if (!string.IsNullOrEmpty(parse))
                parameters.Add(new Tuple<string, string>("parse", parse));

            if (linkNames)
                parameters.Add(new Tuple<string, string>("link_names", "1"));

            if (blocks != null && blocks.Length > 0)
                parameters.Add(new Tuple<string, string>("blocks",
                   JsonConvert.SerializeObject(blocks, Formatting.None,
                      new JsonSerializerSettings // Shouldn't include a not set property
                      {
                          NullValueHandling = NullValueHandling.Ignore
                      })));

            if (attachments != null && attachments.Length > 0)
                parameters.Add(new Tuple<string, string>("attachments",
                    JsonConvert.SerializeObject(attachments, Formatting.None,
                            new JsonSerializerSettings // Shouldn't include a not set property
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            })));

            if (unfurl_links)
                parameters.Add(new Tuple<string, string>("unfurl_links", "1"));

            if (!string.IsNullOrEmpty(icon_url))
                parameters.Add(new Tuple<string, string>("icon_url", icon_url));

            if (!string.IsNullOrEmpty(icon_emoji))
                parameters.Add(new Tuple<string, string>("icon_emoji", icon_emoji));

            if (as_user.HasValue)
                parameters.Add(new Tuple<string, string>("as_user", as_user.ToString()));

            if (!string.IsNullOrEmpty(thread_ts))
                parameters.Add(new Tuple<string, string>("thread_ts", thread_ts));

            APIRequestWithToken(callback, parameters.ToArray());
        }

        public Task<PostMessageResponse> PostMessageAsync(
    string channelId,
    string text,
    string botName = null,
    string parse = null,
    bool linkNames = false,
    IBlock[] blocks = null,
    Attachment[] attachments = null,
    bool unfurl_links = false,
    string icon_url = null,
    string icon_emoji = null,
    bool? as_user = null,
      string thread_ts = null)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            parameters.Add(new Tuple<string, string>("channel", channelId));
            parameters.Add(new Tuple<string, string>("text", text));

            if (!string.IsNullOrEmpty(botName))
                parameters.Add(new Tuple<string, string>("username", botName));

            if (!string.IsNullOrEmpty(parse))
                parameters.Add(new Tuple<string, string>("parse", parse));

            if (linkNames)
                parameters.Add(new Tuple<string, string>("link_names", "1"));

            if (blocks != null && blocks.Length > 0)
                parameters.Add(new Tuple<string, string>("blocks",
                   JsonConvert.SerializeObject(blocks, Formatting.None,
                      new JsonSerializerSettings // Shouldn't include a not set property
                      {
                          NullValueHandling = NullValueHandling.Ignore
                      })));

            if (attachments != null && attachments.Length > 0)
                parameters.Add(new Tuple<string, string>("attachments",
                    JsonConvert.SerializeObject(attachments, Formatting.None,
                            new JsonSerializerSettings // Shouldn't include a not set property
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            })));

            if (unfurl_links)
                parameters.Add(new Tuple<string, string>("unfurl_links", "1"));

            if (!string.IsNullOrEmpty(icon_url))
                parameters.Add(new Tuple<string, string>("icon_url", icon_url));

            if (!string.IsNullOrEmpty(icon_emoji))
                parameters.Add(new Tuple<string, string>("icon_emoji", icon_emoji));

            if (as_user.HasValue)
                parameters.Add(new Tuple<string, string>("as_user", as_user.ToString()));

            if (!string.IsNullOrEmpty(thread_ts))
                parameters.Add(new Tuple<string, string>("thread_ts", thread_ts));

            return APIRequestWithTokenAsync<PostMessageResponse>(parameters.ToArray());
        }

        public void PostEphemeralMessage(
            Action<PostEphemeralResponse> callback,
            string channelId,
            string text,
            string targetuser,
            string parse = null,
            bool linkNames = false,
            Block[] blocks = null,
            Attachment[] attachments = null,
            bool as_user = false,
        string thread_ts = null)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            parameters.Add(new Tuple<string, string>("channel", channelId));
            parameters.Add(new Tuple<string, string>("text", text));
            parameters.Add(new Tuple<string, string>("user", targetuser));

            if (!string.IsNullOrEmpty(parse))
                parameters.Add(new Tuple<string, string>("parse", parse));

            if (linkNames)
                parameters.Add(new Tuple<string, string>("link_names", "1"));

            if (blocks != null && blocks.Length > 0)
                parameters.Add(new Tuple<string, string>("blocks",
                    JsonConvert.SerializeObject(blocks, Formatting.None,
                            new JsonSerializerSettings // Shouldn't include a not set property
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            })));

            if (attachments != null && attachments.Length > 0)
                parameters.Add(new Tuple<string, string>("attachments",
                    JsonConvert.SerializeObject(attachments, Formatting.None,
                            new JsonSerializerSettings // Shouldn't include a not set property
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            })));

            parameters.Add(new Tuple<string, string>("as_user", as_user.ToString()));

            APIRequestWithToken(callback, parameters.ToArray());
        }

        public Task<PostEphemeralResponse> PostEphemeralMessageAsync(
            string channelId,
            string text,
            string targetuser,
            string parse = null,
            bool linkNames = false,
            Block[] blocks = null,
            Attachment[] attachments = null,
            bool as_user = false,
        string thread_ts = null)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            parameters.Add(new Tuple<string, string>("channel", channelId));
            parameters.Add(new Tuple<string, string>("text", text));
            parameters.Add(new Tuple<string, string>("user", targetuser));

            if (!string.IsNullOrEmpty(parse))
                parameters.Add(new Tuple<string, string>("parse", parse));

            if (linkNames)
                parameters.Add(new Tuple<string, string>("link_names", "1"));

            if (blocks != null && blocks.Length > 0)
                parameters.Add(new Tuple<string, string>("blocks",
                    JsonConvert.SerializeObject(blocks, Formatting.None,
                            new JsonSerializerSettings // Shouldn't include a not set property
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            })));

            if (attachments != null && attachments.Length > 0)
                parameters.Add(new Tuple<string, string>("attachments",
                    JsonConvert.SerializeObject(attachments, Formatting.None,
                            new JsonSerializerSettings // Shouldn't include a not set property
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            })));

            parameters.Add(new Tuple<string, string>("as_user", as_user.ToString()));

            return APIRequestWithTokenAsync<PostEphemeralResponse>(parameters.ToArray());
        }

        public void DialogOpen(
           Action<DialogOpenResponse> callback,
           string triggerId,
           Dialog dialog)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            parameters.Add(new Tuple<string, string>("trigger_id", triggerId));

            parameters.Add(new Tuple<string, string>("dialog",
               JsonConvert.SerializeObject(dialog,
                  new JsonSerializerSettings
                  {
                      NullValueHandling = NullValueHandling.Ignore
                  })));

            APIRequestWithToken(callback, parameters.ToArray());
        }

        public void AddReaction(
            Action<ReactionAddedResponse> callback,
            string name = null,
            string channel = null,
            string timestamp = null)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            if (!string.IsNullOrEmpty(name))
                parameters.Add(new Tuple<string, string>("name", name));

            if (!string.IsNullOrEmpty(channel))
                parameters.Add(new Tuple<string, string>("channel", channel));

            if (!string.IsNullOrEmpty(timestamp))
                parameters.Add(new Tuple<string, string>("timestamp", timestamp));

            APIRequestWithToken(callback, parameters.ToArray());
        }

        public Task<ReactionAddedResponse> AddReactionAsync(
    string name = null,
    string channel = null,
    string timestamp = null)
        {
            List<Tuple<string, string>> parameters = new List<Tuple<string, string>>();

            if (!string.IsNullOrEmpty(name))
                parameters.Add(new Tuple<string, string>("name", name));

            if (!string.IsNullOrEmpty(channel))
                parameters.Add(new Tuple<string, string>("channel", channel));

            if (!string.IsNullOrEmpty(timestamp))
                parameters.Add(new Tuple<string, string>("timestamp", timestamp));

            return APIRequestWithTokenAsync<ReactionAddedResponse>(parameters.ToArray());
        }



        public void UploadFile(Action<FileUploadResponse> callback, byte[] fileData, string fileName, string[] channelIds, string title = null, string initialComment = null, bool useAsync = false, string fileType = null)
        {
            Uri target = new Uri(Path.Combine(APIBaseLocation, useAsync ? "files.uploadAsync" : "files.upload"));

            List<string> parameters = new List<string>();
            parameters.Add(string.Format("token={0}", APIToken));

            //File/Content
            if (!string.IsNullOrEmpty(fileType))
                parameters.Add(string.Format("{0}={1}", "filetype", fileType));

            if (!string.IsNullOrEmpty(fileName))
                parameters.Add(string.Format("{0}={1}", "filename", fileName));

            if (!string.IsNullOrEmpty(title))
                parameters.Add(string.Format("{0}={1}", "title", title));

            if (!string.IsNullOrEmpty(initialComment))
                parameters.Add(string.Format("{0}={1}", "initial_comment", initialComment));

            parameters.Add(string.Format("{0}={1}", "channels", string.Join(",", channelIds)));

            using (MultipartFormDataContent form = new MultipartFormDataContent())
            {
                form.Add(new ByteArrayContent(fileData), "file", fileName);
                HttpResponseMessage response = PostRequest(string.Format("{0}?{1}", target, string.Join("&", parameters.ToArray())), form);
                string result = response.Content.ReadAsStringAsync().Result;
                callback(result.Deserialize<FileUploadResponse>());
            }
        }

        public async Task<FileUploadResponse> UploadFileAsync(byte[] fileData, string fileName, string[] channelIds, string title = null, string initialComment = null, bool useAsync = false, string fileType = null)
        {
            Uri target = new Uri(Path.Combine(APIBaseLocation, useAsync ? "files.uploadAsync" : "files.upload"));

            List<string> parameters = new List<string>();
            parameters.Add(string.Format("token={0}", APIToken));

            //File/Content
            if (!string.IsNullOrEmpty(fileType))
                parameters.Add(string.Format("{0}={1}", "filetype", fileType));

            if (!string.IsNullOrEmpty(fileName))
                parameters.Add(string.Format("{0}={1}", "filename", fileName));

            if (!string.IsNullOrEmpty(title))
                parameters.Add(string.Format("{0}={1}", "title", title));

            if (!string.IsNullOrEmpty(initialComment))
                parameters.Add(string.Format("{0}={1}", "initial_comment", initialComment));

            parameters.Add(string.Format("{0}={1}", "channels", string.Join(",", channelIds)));

            using (MultipartFormDataContent form = new MultipartFormDataContent())
            {
                form.Add(new ByteArrayContent(fileData), "file", fileName);
                HttpResponseMessage response = await PostRequestAsync(string.Format("{0}?{1}", target, string.Join("&", parameters.ToArray())), form);
                string result = await response.Content.ReadAsStringAsync();
                return result.Deserialize<FileUploadResponse>();
            }
        }

        public void DeleteFile(Action<FileDeleteResponse> callback, string file = null)
        {
            if (string.IsNullOrEmpty(file))
                return;

            APIRequestWithToken(callback, new Tuple<string, string>("file", file));
        }

        public Task<FileDeleteResponse> DeleteFileAsync(string file)
        {
            if (file == null) throw new ArgumentException(nameof(file));

            return APIRequestWithTokenAsync<FileDeleteResponse>(new Tuple<string, string>("file", file));
        }

        public void UnfurlLink(Action<UnfurlLinkResponse> callback, string channel, string ts, object unfurls)
        {
            var json = JsonConvert.SerializeObject(unfurls);
            APIRequestWithToken(callback,
                new Tuple<string, string>("channel", channel),
                new Tuple<string, string>("ts", ts),
                new Tuple<string, string>("unfurls", json));
        }

        public Task<UnfurlLinkResponse> UnfurlLinkAsync(string channel, string ts, object unfurls)
        {
            var json = JsonConvert.SerializeObject(unfurls);
            return APIRequestWithTokenAsync<UnfurlLinkResponse>(
                new Tuple<string, string>("channel", channel),
                new Tuple<string, string>("ts", ts),
                new Tuple<string, string>("unfurls", json));
        }

        public void JoinChannel(Action<UnfurlLinkResponse> callback, string channel)
        {
            throw new NotImplementedException();
        }

        public Task<UnfurlLinkResponse> JoinChannelAsync(string channel)
        {
            throw new NotImplementedException();
        }
    }
}
