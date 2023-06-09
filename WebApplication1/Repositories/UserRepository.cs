﻿using Microsoft.EntityFrameworkCore;
using TeamProject.Data;
using TeamProject.Models.Domain;

namespace TeamProject.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly MovieDbContext movieDbContext;
        public UserRepository(MovieDbContext movieDbContext)
        {

            this.movieDbContext = movieDbContext;
        }

        public async Task<User> AddAsync(User user)
        {
            await movieDbContext.Users.AddAsync(user);
            await movieDbContext.SaveChangesAsync();
            return user;
        }



        public async Task<IEnumerable<User>> GetAllAsync()
        {
            var user = await movieDbContext.Users.ToListAsync();
            return user.OrderByDescending(x => x.Id).ToList();

        }

        public async Task<User?> GetAsync(long id)
        {
            return await movieDbContext.Users.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<User?> GetByNameAsync(string name)
        {
            var exsistingUser = await movieDbContext.Users.FirstOrDefaultAsync(u => u.Name == name);
            if (exsistingUser == null) return null;

            return exsistingUser;
        }
        public async Task<User?> GetByUserNameAsync(string userName)
        {
            var exsistingUser = await movieDbContext.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (exsistingUser == null) return null;

            return exsistingUser;
        }

        public async Task<User?> DeleteAsync(long id)
        {
            var existingUser = await movieDbContext.Users.FindAsync(id);
            if (existingUser != null)
            {
                movieDbContext.Users.Remove(existingUser);
                await movieDbContext.SaveChangesAsync();
                return existingUser;
            }
            return null;
        }


        public async Task<User> DeleteSelectedUsers(List<long> userIds)
        {


            var existingUsers = await movieDbContext.Users
                .Where(user => userIds.Contains(user.Id))
                .ToListAsync();

            if (existingUsers!=null)
            {
                movieDbContext.Users.RemoveRange(existingUsers);
                await movieDbContext.SaveChangesAsync();
               
            }

            return null;
        }


        public async Task<User?> UpdateAsync(User user)
        {
            var existingUser = await movieDbContext.Users.FindAsync(user.Id);
            if (existingUser == null) return null;

            existingUser.Name = user.Name;
            existingUser.PassWord = user.PassWord;
            existingUser.UserName = user.UserName;
            existingUser.PhoneNum = user.PhoneNum;
            existingUser.NickName = user.NickName;
            existingUser.Email = user.Email;

            await movieDbContext.SaveChangesAsync();
            return existingUser;
        }

        public async Task<User?> UpdatPassAsync(User user)
        {
            var existingUser = await movieDbContext.Users.FindAsync(user.Id);
            if (existingUser == null) return null;

            existingUser.PassWord = user.PassWord;

            await movieDbContext.SaveChangesAsync();
            return existingUser;
        }
    }
}
