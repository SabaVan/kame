using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using backend.UserAuth.Models;

namespace backend.UserAuth.Data
{
    public class UserRepository
    {
        private readonly string _filePath = "users.txt";

        public List<UserModel> GetAllUsers()
        {
            if (!File.Exists(_filePath))
                return new List<UserModel>();

            return File.ReadAllLines(_filePath)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line =>
                {
                    var parts = line.Split('|');
                    // Expect: ID | Username | Hash | Salt
                    return new UserModel(parts[0], parts[1], parts[2], parts[3]);
                })
                .ToList();
        }

        public void SaveUser(UserModel user)
        {
            var record = $"{user.Id}|{user.Username}|{user.PasswordHash}|{user.Salt}";
            File.AppendAllText(_filePath, record + Environment.NewLine);
        }

        public UserModel? GetUserByUsername(string username)
        {
            return GetAllUsers().FirstOrDefault(u => u.Username == username);
        }
    }
}
