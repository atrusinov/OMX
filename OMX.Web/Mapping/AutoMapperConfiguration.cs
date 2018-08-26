﻿using AutoMapper;
using OMX.Common.Property.BindingModels;
using OMX.Models;
using OMX.Web.Areas.Admin.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OMX.Web.Mapping
{
    public class AutoMapperConfiguration : Profile
    {
        public AutoMapperConfiguration()
        {
            this.CreateMap<PropertyBindingModel, Property>();
            //this.CreateMap<User, UsersViewModel>().ForMember(x => x.IsSuspended, opt => opt.Ignore());
            this.CreateMap<Property, PropertyBindingModel>().ForMember(x=> x.Features, x=> x.Ignore());


        }
    }

}
