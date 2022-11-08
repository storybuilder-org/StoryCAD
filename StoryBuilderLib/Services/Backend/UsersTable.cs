using System;

namespace StoryBuilder.Services.Backend;

public class UsersTable
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public DateTime DateAdded { get; set; }
}