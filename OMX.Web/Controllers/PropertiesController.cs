﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using OMX.Common.Property.BindingModels;
using OMX.Common.Property.ViewModels;
using OMX.Models;
using OMX.Services.Contracts;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace OMX.Web.Controllers
{
    [Authorize]
    public class PropertiesController : Controller
    {
        private readonly IPropertyService propertyService;
        private readonly IUserService userService;
        private readonly IMapper mapper;
        private readonly UserManager<User> userManager;
        private readonly IConfiguration config;
        
        public PropertiesController(IPropertyService propertyService, IUserService userService, IMapper mapper, UserManager<User> userManager, IConfiguration config)
        {
            

            this.propertyService = propertyService;
            this.userService = userService;
            this.mapper = mapper;
            this.userManager = userManager;
            this.config = config;
        }

        [HttpGet]
        public IActionResult Create()
        {
            var loggedInUser = HttpContext.User.Identity.Name;

            var user = this.userService.GetUserByEmail(loggedInUser);
            if (user == null || loggedInUser == null)
            {
                return RedirectToAction("NotFound", "Error", new { area = "" });
            }

            if (!user.EmailConfirmed)
            {
                return RedirectToAction("Index", "Identity/Account/Manage");
            }

            var model = new PropertyBindingModel
            {
                Features = propertyService.GetAllFeatures().ToDictionary(x => x.Id, x => x.Name),
                Addresses = propertyService.GetAllAddresses().ToList()
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Create(PropertyBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Create", model);
            }

            var userId = this.userManager.GetUserId(HttpContext.User);
            var property = this.propertyService.CreateProperty(model, userId);

            if (userId == null || property == null)
            {
                return RedirectToAction("NotFound", "Error", new { area = "" });
            }

            SaveImagesToRoot(model, property);

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Details(int id)
        {
            var property = this.propertyService.GetPropertyById(id);
            if (property == null)
            {
                return RedirectToAction("NotFound", "Error", new { area = "" });
            }

            var model = this.mapper.Map<HomePropertiesViewModel>(property);

            return this.View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Details(HomePropertiesViewModel model)
        {
            var userEmail = this.HttpContext.User.Identity.Name;
            var isEmailVerified = this.userService.GetUserByEmail(userEmail).EmailConfirmed;
            if (!isEmailVerified)
            {
                return this.RedirectToAction("Index", "Identity/Account/Manage", new { message = "Please verify your email address!" });
            }
            if (model == null)
            {
                return RedirectToAction("NotFound", "Error", new { area = "" });
            }
            await SendMessasgeToOwner(model.Id, model.Message);
            return this.RedirectToAction("Index", "Home");
        }


        [HttpGet]
        public IActionResult Edit(int id)
        {
            var property = this.propertyService.GetPropertyById(id);
            var model = this.mapper.Map<PropertyBindingModel>(property);

            if (model == null || property == null)
            {
                return RedirectToAction("NotFound", "Error", new { area = "" });
            }

            var userId = this.userManager.GetUserId(HttpContext.User);
            var isAdmin = this.User.IsInRole("Administrator");
            var isModerator = this.User.IsInRole("Moderator");


            if (!isAdmin)
            {
                if (!isModerator)
                {
                    if (property.UserId != userId)
                    {
                        return RedirectToAction("MyListings", "Users");
                    }
                }
            }

            model.Features = propertyService.GetAllFeatures().ToDictionary(x => x.Id, x => x.Name);
            model.Addresses = propertyService.GetAllAddresses().ToList();
            model.SelectedFeatures = propertyService.GetAllSelectedFeatures(id).ToList();

            return View(model);

        }
        [HttpPost]
        public IActionResult Edit(PropertyBindingModel model)
        {
            if (model == null)
            {
                return RedirectToAction("NotFound", "Error", new { area = "" });
            }
            if (!this.ModelState.IsValid)
            {
                model.Features = propertyService.GetAllFeatures().ToDictionary(x => x.Id, x => x.Name);
                model.Addresses = propertyService.GetAllAddresses().ToList();
                model.SelectedFeatures = propertyService.GetAllSelectedFeatures(model.Id).ToList();

                return View(model);
            }
            this.propertyService.EditProperty(model.Id, model);
            var property = this.propertyService.GetPropertyById(model.Id);

            if (model.Images.Any())
            {
                SaveImagesToRoot(model, property);
            }

            return RedirectToAction("Details", new { id = model.Id });
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var property = this.propertyService.GetPropertyById(id);
            var model = this.mapper.Map<HomePropertiesViewModel>(property);

            if (model == null || property == null)
            {
                return RedirectToAction("NotFound", "Error", new { area = "" });
            }

            var userId = this.userManager.GetUserId(HttpContext.User);
            var isAdmin = this.User.IsInRole("Administrator");
            var isModerator = this.User.IsInRole("Moderator");

            if (!isAdmin)
            {
                if (!isModerator)
                {
                    if (property.UserId != userId)
                    {
                        return RedirectToAction("MyListings", "Users");
                    }
                }
            }

            return View(model);
        }
        [HttpPost]
        public IActionResult Delete(PropertyBindingModel model)
        {
            try
            {
                this.propertyService.DeletePropertyById(model.Id);
            }
            catch (Exception)
            {

                return RedirectToAction("NotFound", "Error", new { area = "" });
            }


            return RedirectToAction("Index", "Home");
        }
        private void SaveImagesToRoot(PropertyBindingModel model, Property property)
        {
            foreach (var image in model.Images.Take(3))
            {

                if (image.ContentType == "image/jpeg")
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{property.Id}");

                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }

                    var fileStream = new FileStream(Path.Combine(filePath, image.FileName), FileMode.Create);

                    image.CopyTo(fileStream);
                    fileStream.Close();
                }

            }
        }
        private async Task SendMessasgeToOwner(int id, string message)
        {

            var property = this.propertyService.GetPropertyById(id);
            if (property == null)
            {
                return;
            }

            var senderEmail = this.HttpContext.User.Identity.Name;
            var receivingEmail = property.User.Email;

            var apiKey = this.config.GetSection("ExternalAuth:SendGrid:ApiKey").Value;
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(senderEmail, senderEmail);
            var subject = $"You have a new message from OMX - {senderEmail}";
            var to = new EmailAddress(receivingEmail);
            var plainTextContent = message;
            var htmlContent = message;
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
        }
    }


}


