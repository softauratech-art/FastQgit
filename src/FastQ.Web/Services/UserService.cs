using FastQ.Data.Common;
using FastQ.Data.Db;
using FastQ.Data.Repositories;
using FastQ.Web.Models.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FastQ.Web.Services
{
    public class UserService
    {
        private readonly IUserRepository _users;
        private readonly string _stampuser = new AuthService().GetLoggedInWindowsUser();
        private Int32 _sessionentity  = new AuthService().GetSessionEntityId();
        public UserService()
           : this(
               DbRepositoryFactory.CreateUserRepository())
        {
        }
 
        public UserService(IUserRepository users)
        {
            _users = users;
        }
        public IList<UserVM> ListUsers()
        {
            //if (HttpContext.Current.Session["fq_this_entity"]  == null  ||
            //        !Int32.TryParse(HttpContext.Current.Session["fq_this_entity"].ToString(), out _stampuserentity))
            //    throw new Exception("Entity is missing for this session"); 
            
            return TransformToModelList();
        }

        public UserVM GetUser(string userid)
        {
            try
            {
                var usr = _users.Get(userid, _stampuser);
                return new UserVM
                {
                    FirstName = usr.FirstName,
                    LastName = usr.LastName,
                    UserId = usr.UserId,
                    Title = usr.Title,
                    OtherLanguage = usr.Language,
                    IsActive = usr.ActiveFlag,
                    //todo IsAdmin = usr.AdminFlag,
                    Email = usr.Email
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public Result HandleUserAction(string action, string json)
        {
            //action = (action ?? string.Empty).Trim().ToLowerInvariant();
            //return action switch
            //{
            //    "update" => UpdateUser( json),
            //    "delete" => DeleteUser( json),
            //    "create" => CreateUser( json),
            //    _ => Result.Fail("Unknown action")
            //};

            return Result.Fail("Not Implemented");
        }

        public IList<UserVM> TransformToModelList()
        {
            if (string.IsNullOrWhiteSpace(_stampuser))
            {
                return new List<UserVM>();
            }
                        
            var rows = _users.ListAll(_sessionentity, _stampuser);

            return rows.Select(r =>
            {               
                return new UserVM
                {
                    FirstName = r.FirstName,
                    LastName = r.LastName,
                    UserId = r.UserId,
                    Title = r.Title,
                    OtherLanguage = r.Language,
                    IsActive = r.ActiveFlag,
                    //todo IsAdmin = r.AdminFlag,
                    Email = r.Email             
                };
            }).OrderBy(r => r.LastName).ToList();
        }

    }
}
