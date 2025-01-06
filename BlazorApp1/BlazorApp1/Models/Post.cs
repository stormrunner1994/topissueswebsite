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

        public Post()
        {

        }
    }
}
