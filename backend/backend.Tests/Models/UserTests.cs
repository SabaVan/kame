using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;
using backend.Models;

namespace backend.Tests.Models
{
    public class UserTests
    {
        private static IList<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var ctx = new ValidationContext(model, serviceProvider: null, items: null);
            Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
            return results;
        }

        [Fact]
        public void DefaultConstructor_SetsDefaults()
        {
            var user = new User();

            Assert.NotEqual(Guid.Empty, user.Id);
            Assert.Equal(string.Empty, user.Username);
            Assert.Equal(string.Empty, user.PasswordHash);
            Assert.NotNull(user.Credits);
            Assert.Equal(string.Empty, user.Salt);
        }

        [Fact]
        public void ParameterizedConstructor_AssignsValues()
        {
            var id = Guid.NewGuid();
            var user = new User(id, "bob", "hashedpwd", "somesalt");

            Assert.Equal(id, user.Id);
            Assert.Equal("bob", user.Username);
            Assert.Equal("hashedpwd", user.PasswordHash);
            Assert.Equal("somesalt", user.Salt);
            Assert.NotNull(user.Credits);
        }

        [Fact]
        public void Validation_Fails_When_Username_Is_Empty()
        {
            var user = new User
            {
                Username = "",
                PasswordHash = "notempty"
            };

            var results = ValidateModel(user);
            Assert.Contains(results, r => r.MemberNames != null && System.Linq.Enumerable.Contains(r.MemberNames, "Username"));
        }

        [Fact]
        public void Validation_Fails_When_PasswordHash_Is_Empty()
        {
            var user = new User
            {
                Username = "valid",
                PasswordHash = ""
            };

            var results = ValidateModel(user);
            Assert.Contains(results, r => r.MemberNames != null && System.Linq.Enumerable.Contains(r.MemberNames, "PasswordHash"));
        }

        [Fact]
        public void Validation_Fails_When_Username_Too_Long()
        {
            var longName = new string('a', 51); // MaxLength is 50
            var user = new User
            {
                Username = longName,
                PasswordHash = "notempty"
            };

            var results = ValidateModel(user);
            Assert.Contains(results, r => r.MemberNames != null && System.Linq.Enumerable.Contains(r.MemberNames, "Username"));
        }

        [Fact]
        public void Validation_Passes_For_Valid_User()
        {
            var user = new User
            {
                Username = "alice",
                PasswordHash = "securehash"
            };

            var results = ValidateModel(user);
            Assert.Empty(results);
        }
    }
}
