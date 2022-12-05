using System.Runtime.Serialization;

namespace WakaTime
{
    [DataContract]
    public class GithubReleaseApiResponse
    {
        [DataMember(Name = "tag_name")]
        public string TagName { get; set; }
    }
}
