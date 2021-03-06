﻿using System;

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Security;
using TeamThing.Model.Helpers;
using TeamThing.Web.Core.Helpers;
using TeamThing.Web.Core.Mappers;
using TeamThing.Web.Core.Security;
using DomainModel = TeamThing.Model;
using ServiceModel = TeamThing.Web.Models.API;

namespace TeamThing.Web.Controllers
{
    public class UserController : TeamThingApiController
    {
        public UserController()
            : base(new TeamThing.Model.TeamThingContext())
        {
        }

        //[Authorize]
        [Queryable]
        public IQueryable<ServiceModel.UserBasic> Get()
        {
            return context.GetAll<DomainModel.User>().MapToServiceModel();
        }

        // GET /api/user/5
        //[Authorize]
        public HttpResponseMessage Get(int id)
        {
            var item = context.GetAll<TeamThing.Model.User>()
                              .FirstOrDefault(u => u.Id == id);
            if (item == null)
            {
                ModelState.AddModelError("", "Invalid User");
                return Request.CreateResponse(HttpStatusCode.BadRequest, ModelState.ToJson());
            }

            var sUser = item.MapToServiceModel();
            var response = Request.CreateResponse(HttpStatusCode.OK, sUser);
            response.Headers.Location = new Uri(Request.RequestUri, "/api/user/" + sUser.Id.ToString());
            return response;
        }

        // GET /api/user/5/things/{status}
        //[Authorize]
        [HttpGet]
        [Queryable]
        public IQueryable<ServiceModel.ThingBasic> Things(int id, string status)
        {
            var user = context.GetAll<DomainModel.User>().FirstOrDefault(u => u.Id == id);
            if (user == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid User"));

            if (status != null)
            {
                return user.Things
                           .WithStatus(status)
                           .MapToBasicServiceModel()
                           .AsQueryable();
            }

            return user.Things
                       .Active()
                       .MapToBasicServiceModel()
                       .AsQueryable();

        }
        
        [HttpGet]
        [Queryable]
        public IQueryable<ServiceModel.ThingBasic> Starred(int id, int teamId)
        {
            var user = context.GetAll<DomainModel.User>().FirstOrDefault(u => u.Id == id);
            if (user == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid User"));

            var team = user.Teams.FirstOrDefault(t => t.TeamId == teamId);
            if (team == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid Team"));

            var teamThingIds = team.Team.Things.Select(x => x.Id);

            return user.StarredThings.Where(t => teamThingIds.Contains(t.Id)).MapToBasicServiceModel().AsQueryable();

        }

        // GET /api/user/5/teams/{status}
        //[Authorize]
        [HttpGet]
        [Queryable]
        public IQueryable<ServiceModel.TeamBasic> Teams(int id, string status)
        {
            var user = context.GetAll<DomainModel.User>().FirstOrDefault(u => u.Id == id);
            if (user == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid User"));

            if (status != null)
            {
                return user.Teams
                           .TeamMembersWithStatus(status)
                           .MapToBasicServiceModel()
                           .AsQueryable();
            }

            return user.Teams
                       .ActiveTeamMembers()
                       .MapToBasicServiceModel()
                       .AsQueryable();
        }

        [HttpPost, Obsolete("Use oauth sign in")]
        public HttpResponseMessage SignIn(ServiceModel.SignInViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ModelState.ToJson());
            }

            var existingUser = context.GetAll<DomainModel.User>()
                                      .FirstOrDefault(u => u.EmailAddress.Equals(model.EmailAddress, StringComparison.OrdinalIgnoreCase));

            if (existingUser == null)
            {
                ModelState.AddModelError("", "A user does not exist with this user name.");
                return Request.CreateResponse(HttpStatusCode.BadRequest, ModelState.ToJson());
            }

            return Request.CreateResponse(HttpStatusCode.OK, existingUser.MapToServiceModel());
        }

        //POST /api/user
        [HttpPost]
        public HttpResponseMessage OAuth(ServiceModel.OAuthSignInModel model)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ModelState.ToJson());
            }

            //validate user
            var provider = AuthFactory.GetProvider(model.Provider, model.AuthToken);
            var userInfo = provider.GetUser();
            string userId = userInfo.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                ModelState.AddModelError("", string.Format("{0} could not locate a user using the provided auth token."));
                return Request.CreateResponse(HttpStatusCode.Unauthorized, ModelState.ToJson());
            }

            //get actual user
            var user = context.GetAll<DomainModel.User>()
                              .FirstOrDefault(u => u.OAuthProvider.Equals(model.Provider, StringComparison.OrdinalIgnoreCase) && u.OAuthUserId.Equals(userId, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                //try to find users by existing email address (mostly to clean up v1)
                if (!string.IsNullOrWhiteSpace(userInfo.Email))
                {
                    user = context.GetAll<DomainModel.User>()
                                  .FirstOrDefault(u => u.EmailAddress.Equals(userInfo.Email, StringComparison.OrdinalIgnoreCase));
                }

                //user really is new, lets create them
                if (user == null)
                {
                    user = new DomainModel.User(model.Provider, userId);
                    context.Add(user);
                }

                user.EmailAddress = userInfo.Email;
                user.ImagePath = userInfo.PictureUrl;
                user.FirstName = userInfo.FirstName;
                user.LastName = userInfo.LastName;

                if (string.IsNullOrWhiteSpace(user.ImagePath))
                {
                    var defaultImage = new Uri(Request.RequestUri, "/images/GenericUserImage.gif");
                    user.ImagePath = defaultImage.ToString();
                }

                context.SaveChanges();
            }

            //FormsAuthentication.SetAuthCookie(user.EmailAddress, true);
            return Request.CreateResponse(HttpStatusCode.OK, user.MapToServiceModel());
        }

        [HttpPost, Obsolete("Use oauth sign in")]
        public HttpResponseMessage Register(ServiceModel.AddUserModel value)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ModelState.ToJson());
            }

            var existingUser = context.GetAll<DomainModel.User>()
                                   .FirstOrDefault(u => u.EmailAddress.Equals(value.EmailAddress, StringComparison.OrdinalIgnoreCase));

            if (existingUser != null)
            {
                ModelState.AddModelError("", "A user with this email address has already registered!");
                return Request.CreateResponse(HttpStatusCode.BadRequest, ModelState.ToJson());
            }

            var user = new DomainModel.User(value.EmailAddress);
            var defaultImage = new Uri(Request.RequestUri, "/images/GenericUserImage.gif");
            user.ImagePath = defaultImage.ToString();
            context.Add(user);
            context.SaveChanges();

            var sUser = user.MapToServiceModel();
            var response = Request.CreateResponse(HttpStatusCode.Created, sUser);
            response.Headers.Location = new Uri(Request.RequestUri, "/api/user/" + sUser.Id.ToString());
            return response;
        }

        // PUT /api/user/5
        //[Authorize]
        //public void Put(int id, string value)
        //{
        //    var user = context.GetAll<TeamThing.Model.User>()
        //                      .FirstOrDefault(u => u.Id == id);

        //    if (user == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid User"));
        //}

        // DELETE /api/user/5
        //[Authorize]
        public HttpResponseMessage Delete(int id)
        {
            var user = context.GetAll<TeamThing.Model.User>()
                              .FirstOrDefault(u => u.Id == id);

            if (user == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid User"));

            context.Delete(user);
            context.SaveChanges();


            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }
    }
}
