namespace BlazorApp1.wwwroot.Classes
{
    public class Application
    {
        public Postgres Database = new Postgres(false);
        public string Cache = "";

        public Application()
        {

        }

        public void Start()
        {

        }


    }
}
