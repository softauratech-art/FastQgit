using System;
using System.Collections.Generic;
using System.Linq;
using FastQ.Data.Db;
using FastQ.Data.Repositories;
using FastQ.Web.Models.Admin;

namespace FastQ.Web.Services
{
    public class UserService
    {
        private readonly IUserRepository _users;
        private readonly string _stampuser = new AuthService().GetLoggedInWindowsUser();
        private readonly long _sessionentity  = new AuthService().GetSessionEntityId();
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
            return TransformToModelList();
        }

        public UserVM GetUser(string userid)
        {
            try
            {
                var usr = _users.Get(userid, _stampuser);
                bool configadmin = usr.BusinessEntities.FirstOrDefault(e => e.EntityId == _sessionentity).ConfigAdminFlag;

                return TransformToModel(usr);
            }
            catch
            {
                return null;
            }
        }

        private UserVM TransformToModel(Data.Entities.User userentity) {
            UserVM ouser = new UserVM
            {
                FirstName = userentity.FirstName,
                LastName = userentity.LastName,
                UserId = userentity.UserId,
                Title = userentity.Title,
                OtherLanguage = userentity.Language,
                IsActive = userentity.ActiveFlag,
                Email = userentity.Email
            };

            if (userentity.BusinessEntities != null)
                ouser.IsAdmin = userentity.BusinessEntities.FirstOrDefault(e => e.EntityId == _sessionentity).ConfigAdminFlag;
            
            if (userentity.Queues != null)
                ouser.Permissions = userentity.Queues?.Where(q => q.EntityId == _sessionentity && q.QueueActiveFlag == true).ToList();
          
            return ouser;
        }
        public IList<UserVM> TransformToModelList()
        {
            if (string.IsNullOrWhiteSpace(_stampuser))
                return new List<UserVM>();
                        
            var rows = _users.ListAll(_sessionentity, _stampuser, false);
            return rows.Select(r =>
            {
                return TransformToModel(r);
            }).OrderBy(r => r.LastName).ToList();
        }

        public void AddOrUpdateUser(string action, UserVM uvm, string hostqueues, string providerqueues, string reporterqueues, string queueadminqueues)
        {
            List<Data.Entities.UserEntity> entities = [];
            entities.Add(new Data.Entities.UserEntity { EntityId = _sessionentity, ConfigAdminFlag = uvm.IsAdmin, ActiveFlag = uvm.IsActive });
            
            _users.AddOrUpdateUser(action, 
                                    new Data.Entities.User {
                                        UserId = uvm.UserId, FirstName = uvm.FirstName, LastName = uvm.LastName, 
                                        Email = uvm.Email, Phone = uvm.Phone, ActiveFlag = uvm.IsActive, 
                                        Title = uvm.Title, Language = uvm.OtherLanguage, BusinessEntities = entities
                                    }, 
                                    _sessionentity,                        
                                    hostqueues, providerqueues, reporterqueues, queueadminqueues,
                                    _stampuser);
        }

        public void Delete(string uid)
        {
            _users.Delete(uid, _sessionentity, _stampuser);
        }
    }
}
