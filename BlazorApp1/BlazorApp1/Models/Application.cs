﻿using BlazorApp1.wwwroot.Classes;

namespace BlazorApp1.Models
{
    public class Application
    {
        public Postgres Database = new Postgres(false);
        public string Cache = "";
        public bool IsRunning = false;

        public Application()
        {
            Task.Run(() => TaskConnect());
        }

        private Task TaskConnect()
        {
            string error = "";
            IsRunning = Database.Connect(ref error);
            return Task.CompletedTask;
        }
    }
}
