using FastQ.Data.Common;
using FastQ.Data.Db;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;
using FastQ.Web.Helpers;
using FastQ.Web.Models;
using FastQ.Web.Models.Admin;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Cache;
using System.Web;
using System.Web.ModelBinding;
using System.Web.Mvc;

namespace FastQ.Web.Services
{
    public class UserService
    {
        private readonly IUserRepository _users;
        private readonly string _stampuser = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToLower().Replace("ocgov\\", "");
        private Int32 _stampuserentity;
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
            if (HttpContext.Current.Session["fq_this_entity"]  == null  ||
                    !Int32.TryParse(HttpContext.Current.Session["fq_this_entity"].ToString(), out _stampuserentity))
                throw new Exception("Entity is missing for this session"); 
            
            return TransformToModelList();
        }

        public UserVM GetUser(string userid)
        {           
            var usr = _users.Get(userid, _stampuser);
            return new UserVM
            {
                FirstName = usr.FirstName,
                LastName = usr.LastName,
                UserId = usr.UserId,
                Title = usr.Title,
                OtherLanguage = usr.Language,
                IsActive =  usr.ActiveFlag,
                IsAdmin = usr.AdminFlag,
                Email = usr.Email
            };
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
                        
            var rows = _users.ListAll(_stampuserentity, _stampuser);

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
                    IsAdmin = r.AdminFlag,
                    Email = r.Email             
                };
            }).OrderBy(r => r.LastName).ToList();
        }

    }
}
