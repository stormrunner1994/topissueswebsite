namespace BlazorApp1.Models
{
    public class Post
    {
        public int Id = 0;
        public string Title = "";
        public string Content = "";
        public string Topic = "";
        public string Author = "";
        public int ReactedToPostId = 0;
        public int Likes = 0;
        public int Dislikes = 0;
        public DateTime CreationDate = DateTime.MinValue;
        public DateTime LastModificationDate = DateTime.MinValue;
        public List<Post> Reactions = new List<Post>();

        public Post(int id, string title, string content, string topic, string author, int reactedToPostId,
            int likes, int dislikes, DateTime creationdate, DateTime lastModificationDate)
        {
            Id = id;
            Title = title;
            Content = content;
            Topic = topic;
            Author = author;
            Likes = likes;
            Dislikes = dislikes;
            ReactedToPostId = reactedToPostId;
            CreationDate = creationdate;
            LastModificationDate = lastModificationDate;
        }
    }
}
