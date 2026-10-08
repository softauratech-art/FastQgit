using System;
using System.Collections.Generic;
using FastQ.Data.Entities;
using FastQ.Web.Helpers;
class RoleChecks
{
 static void Check(bool actual,bool expected,string name) { if(actual!=expected)throw new Exception(name);Console.WriteLine("PASS: "+name); }
 static void Main()
 {
  var member=new UserEntity { EntityId=1 };
  var queue=new UserQueuePermission { EntityId=1,QueueId=10,QueueActiveFlag=true,LobbyFlag=true };
  var user=new User { BusinessEntities=new List<UserEntity>{member},Queues=new List<UserQueuePermission>{queue} };
  Check(LobbyRolePolicy.IsLobbyOnly(user,1),true,"Lobby-only account restricted");
  Check(LobbyRolePolicy.IsLobbyOnly(user,0),true,"Unselected entity still identified as lobby-only");
  Check(LobbyRolePolicy.IsLobbyOnly(user,2),false,"Other entity grants no lobby role");
  queue.ReporterFlag=true;Check(LobbyRolePolicy.IsLobbyOnly(user,1),false,"Reporter + Lobby retains reporter access");queue.ReporterFlag=false;
  queue.HostFlag=true;Check(LobbyRolePolicy.IsLobbyOnly(user,1),false,"Host + Lobby retains host access");queue.HostFlag=false;
  queue.ProviderFlag=true;Check(LobbyRolePolicy.IsLobbyOnly(user,1),false,"Provider + Lobby retains provider access");queue.ProviderFlag=false;
  queue.QueueAdminFlag=true;Check(LobbyRolePolicy.IsLobbyOnly(user,1),false,"Queue admin retains access");queue.QueueAdminFlag=false;
  member.ConfigAdminFlag=true;Check(LobbyRolePolicy.IsLobbyOnly(user,1),false,"Super admin retains access");member.ConfigAdminFlag=false;
  member.ActiveFlag=false;Check(LobbyRolePolicy.IsLobbyOnly(user,1),false,"Inactive membership grants no role");member.ActiveFlag=true;
  queue.QueueActiveFlag=false;Check(LobbyRolePolicy.IsLobbyOnly(user,1),false,"Inactive queue grants no role");queue.QueueActiveFlag=true;
  user.ActiveFlag=false;Check(LobbyRolePolicy.IsLobbyOnly(user,1),false,"Inactive user grants no role");
  Check(LobbyRolePolicy.IsLobbyOnly(null,1),false,"Missing user grants no role");
 }
}
