﻿
using cloudscribe.Core.Models;
using cloudscribe.Core.Models.Geography;
using cloudscribe.Core.Web;
using Microsoft.Owin;
using cloudscribe.Core.Repositories.Caching;
using Microsoft.Owin.Security.DataProtection;
//using Ninject;
//using Ninject.Web.Common;
//using Ninject.Web.Common.OwinHost;
//using Ninject.Web;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Integration.Owin;
using Autofac.Integration.Mvc;
using Autofac.Integration.WebApi;
using Owin;
using System;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Http;
using log4net;
using MvcSiteMapProvider;
using MvcSiteMapProvider.Xml;
using MvcSiteMapProvider.Loader;
//using cloudscribe.WebHost.DI;
//using cloudscribe.WebHost.DI.Ninject.Modules;
using cloudscribe.Configuration;

//http://www.codemag.com/Article/1405071


[assembly: OwinStartupAttribute(typeof(cloudscribe.WebHost.Startup))]
namespace cloudscribe.WebHost
{
    public partial class Startup
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Startup));

        private IDataProtectionProvider dataProtectionProvider = null;
        private IContainer container = null;
        private ISiteContext _siteContext = null;

        public void Configuration(IAppBuilder app)
        {
            //http://benfoster.io/blog/how-to-write-owin-middleware-in-5-different-steps

            //app.CreatePerOwinContext(GetKernel);
            //app.UseNinjectMiddleware(GetKernel);

            var builder = new ContainerBuilder();
            RegisterServices(builder);
            builder.RegisterControllers(Assembly.GetExecutingAssembly()).InstancePerRequest();
            builder.RegisterControllers(typeof(cloudscribe.Core.Web.SiteContext).Assembly).InstancePerRequest();

            //builder.RegisterModule(new cloudscribe.WebHost.DI.Autofac.Modules.MvcSiteMapProviderModule()); // Required
            builder.RegisterModule(new cloudscribe.WebHost.DI.Autofac.Modules.MvcModule());

            container = builder.Build();
            //container.
            
            

            // this needed by SiteUserManager so must be set on the sitecontext first
            dataProtectionProvider = app.GetDataProtectionProvider();

            ISiteContext siteContext = GetSiteContext();

            var newBuilder = new ContainerBuilder();
            newBuilder.RegisterInstance(siteContext).As<ISiteContext>();
            newBuilder.Update(container);

            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
            
            
            app.CreatePerOwinContext(GetSiteContext);

            //app.UseAutofacMiddleware(container);


            //HttpConfiguration config = new HttpConfiguration();
            //app.UseAutofacWebApi(GlobalConfiguration.Configuration);

            // Setup global sitemap loader (required)
            //MvcSiteMapProvider.SiteMaps.Loader = container.Resolve<ISiteMapLoader>();

            // Check all configured .sitemap files to ensure they follow the XSD for MvcSiteMapProvider (optional)
            //var validator = container.Resolve<ISiteMapXmlValidator>();
            //validator.ValidateXml(HostingEnvironment.MapPath("~/Mvc.sitemap"));

            //config.DependencyResolver = new NinjectDependencyResolver(CreateKernel());

            //app.UseWebApi(config);

            // unfortunately this does not seem to work
            // it changes the Request Path to one that matches a rout but still
            // ends in 404
            //app.Use(typeof(UrlRewriterMiddleware), GetSiteRepository());
            //app.UseStageMarker(PipelineStage.Authenticate);

            
            
            ConfigureAuth(app);
        }

        private ISiteContext GetSiteContext()
        {
            if (_siteContext != null) { return _siteContext; }
            //StandardKernel ninjectKernal = GetKernel();
            //ISiteRepository siteRepo = ninjectKernal.Get<ISiteRepository>();   
            ISiteRepository siteRepo = container.Resolve<ISiteRepository>();
            CachingSiteRepository siteCache = new CachingSiteRepository(siteRepo);

            //IUserRepository userRepo = ninjectKernal.Get<IUserRepository>(); 
            IUserRepository userRepo = container.Resolve<IUserRepository>(); 
            CachingUserRepository userCache = new CachingUserRepository(userRepo);

            _siteContext
                = new SiteContext(siteCache, userCache, dataProtectionProvider);
            
            return _siteContext;
        }

        //private static ISiteRepository GetSiteRepository()
        //{
            
        //    // TODO : dependency injection
        //    return SiteContext.GetSiteRepository();
            
        //}



        //private static IUserRepository GetUserRepository()
        //{
        //    // TODO : dependency injection
        //    return SiteContext.GetUserRepository();

        //} 

        //private static StandardKernel _kernel = null;
        //public static StandardKernel GetKernel()
        //{
        //    if (_kernel == null)
        //    {
        //        _kernel = new StandardKernel();
        //        _kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);

        //        _kernel.Load(Assembly.GetExecutingAssembly());
        //        //_kernel.Load(Assembly.GetExecutingAssembly(), Assembly.Load("Super.CompositionRoot"));

                

        //        _kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();


                
        //        RegisterServices(_kernel);

        //        if (AppSettings.MvcSiteMapProvider_UseExternalDIContainer)
        //        {
        //            BindSiteMapProvider(_kernel);
        //        }
                
        //    }
        //    return _kernel;
        //}


        private static void RegisterServices(ContainerBuilder builder)
        {
            

            // here we could use conditional compilation to map alternate data layers
            //
            //kernel.Bind<ISiteContext>().To<cloudscribe.Core.Web.SiteContext>(); 

            
            switch(AppSettings.DbPlatform.ToLower())
            {
                case "sqlite":

                    //kernel.Bind<ISiteRepository>().To<cloudscribe.Core.Repositories.SQLite.SiteRepository>();
                    //kernel.Bind<IUserRepository>().To<cloudscribe.Core.Repositories.SQLite.UserRepository>();
                    //kernel.Bind<IGeoRepository>().To<cloudscribe.Core.Repositories.SQLite.GeoRepository>();
                    //kernel.Bind<IDb>().To<cloudscribe.DbHelpers.SQLite.Db>();
                    var siteRepo1 = new cloudscribe.Core.Repositories.SQLite.SiteRepository();
                    builder.RegisterInstance(siteRepo1).As<ISiteRepository>();

                    var userRepo1 = new cloudscribe.Core.Repositories.SQLite.UserRepository();
                    builder.RegisterInstance(userRepo1).As<IUserRepository>();

                    var geoRepo1 = new cloudscribe.Core.Repositories.SQLite.GeoRepository();
                    builder.RegisterInstance(geoRepo1).As<IGeoRepository>();

                    var db1 = new cloudscribe.DbHelpers.SQLite.Db();
                    builder.RegisterInstance(db1).As<IDb>();
                    
                    break;

                case "sqlce":

                    //kernel.Bind<ISiteRepository>().To<cloudscribe.Core.Repositories.SqlCe.SiteRepository>();
                    //kernel.Bind<IUserRepository>().To<cloudscribe.Core.Repositories.SqlCe.UserRepository>();
                    //kernel.Bind<IGeoRepository>().To<cloudscribe.Core.Repositories.SqlCe.GeoRepository>();
                    //kernel.Bind<IDb>().To<cloudscribe.DbHelpers.SqlCe.Db>();

                    var siteRepo2 = new cloudscribe.Core.Repositories.SqlCe.SiteRepository();
                    builder.RegisterInstance(siteRepo2).As<ISiteRepository>();

                    var userRepo2 = new cloudscribe.Core.Repositories.SqlCe.UserRepository();
                    builder.RegisterInstance(userRepo2).As<IUserRepository>();

                    var geoRepo2 = new cloudscribe.Core.Repositories.SqlCe.GeoRepository();
                    builder.RegisterInstance(geoRepo2).As<IGeoRepository>();

                    var db2 = new cloudscribe.DbHelpers.SqlCe.Db();
                    builder.RegisterInstance(db2).As<IDb>();

                    break;

                case "pgsql":

                    //kernel.Bind<ISiteRepository>().To<cloudscribe.Core.Repositories.pgsql.SiteRepository>();
                    //kernel.Bind<IUserRepository>().To<cloudscribe.Core.Repositories.pgsql.UserRepository>();
                    //kernel.Bind<IGeoRepository>().To<cloudscribe.Core.Repositories.pgsql.GeoRepository>();
                    //kernel.Bind<IDb>().To<cloudscribe.DbHelpers.pgsql.Db>();

                    var siteRepo3 = new cloudscribe.Core.Repositories.pgsql.SiteRepository();
                    builder.RegisterInstance(siteRepo3).As<ISiteRepository>();

                    var userRepo3 = new cloudscribe.Core.Repositories.pgsql.UserRepository();
                    builder.RegisterInstance(userRepo3).As<IUserRepository>();

                    var geoRepo3 = new cloudscribe.Core.Repositories.pgsql.GeoRepository();
                    builder.RegisterInstance(geoRepo3).As<IGeoRepository>();

                    var db3 = new cloudscribe.DbHelpers.pgsql.Db();
                    builder.RegisterInstance(db3).As<IDb>();

                    break;

                case "firebird":

                    //kernel.Bind<ISiteRepository>().To<cloudscribe.Core.Repositories.Firebird.SiteRepository>();
                    //kernel.Bind<IUserRepository>().To<cloudscribe.Core.Repositories.Firebird.UserRepository>();
                    //kernel.Bind<IGeoRepository>().To<cloudscribe.Core.Repositories.Firebird.GeoRepository>();
                    //kernel.Bind<IDb>().To<cloudscribe.DbHelpers.Firebird.Db>();
                    var siteRepo4 = new cloudscribe.Core.Repositories.Firebird.SiteRepository();
                    builder.RegisterInstance(siteRepo4).As<ISiteRepository>();

                    var userRepo4 = new cloudscribe.Core.Repositories.Firebird.UserRepository();
                    builder.RegisterInstance(userRepo4).As<IUserRepository>();

                    var geoRepo4 = new cloudscribe.Core.Repositories.Firebird.GeoRepository();
                    builder.RegisterInstance(geoRepo4).As<IGeoRepository>();

                    var db4 = new cloudscribe.DbHelpers.Firebird.Db();
                    builder.RegisterInstance(db4).As<IDb>();

                    break;

                case "mysql":

                    //kernel.Bind<ISiteRepository>().To<cloudscribe.Core.Repositories.MySql.SiteRepository>();
                    //kernel.Bind<IUserRepository>().To<cloudscribe.Core.Repositories.MySql.UserRepository>();
                    //kernel.Bind<IGeoRepository>().To<cloudscribe.Core.Repositories.MySql.GeoRepository>();
                    //kernel.Bind<IDb>().To<cloudscribe.DbHelpers.MySql.Db>();

                    var siteRepo5 = new cloudscribe.Core.Repositories.MySql.SiteRepository();
                    builder.RegisterInstance(siteRepo5).As<ISiteRepository>();

                    var userRepo5 = new cloudscribe.Core.Repositories.MySql.UserRepository();
                    builder.RegisterInstance(userRepo5).As<IUserRepository>();

                    var geoRepo5 = new cloudscribe.Core.Repositories.MySql.GeoRepository();
                    builder.RegisterInstance(geoRepo5).As<IGeoRepository>();

                    var db5 = new cloudscribe.DbHelpers.MySql.Db();
                    builder.RegisterInstance(db5).As<IDb>();

                    break;

                case "mssql":
                default:

                    //kernel.Bind<ISiteRepository>().To<cloudscribe.Core.Repositories.MSSQL.SiteRepository>();
                    //kernel.Bind<IUserRepository>().To<cloudscribe.Core.Repositories.MSSQL.UserRepository>();
                    //kernel.Bind<IGeoRepository>().To<cloudscribe.Core.Repositories.MSSQL.GeoRepository>();
                    //kernel.Bind<IDb>().To<cloudscribe.DbHelpers.MSSQL.Db>();

                    var siteRepo6 = new cloudscribe.Core.Repositories.MSSQL.SiteRepository();
                    builder.RegisterInstance(siteRepo6).As<ISiteRepository>();

                    var userRepo6 = new cloudscribe.Core.Repositories.MSSQL.UserRepository();
                    builder.RegisterInstance(userRepo6).As<IUserRepository>();

                    var geoRepo6 = new cloudscribe.Core.Repositories.MSSQL.GeoRepository();
                    builder.RegisterInstance(geoRepo6).As<IGeoRepository>();

                    var db6 = new cloudscribe.DbHelpers.MSSQL.Db();
                    builder.RegisterInstance(db6).As<IDb>();

                    break;

            }

        } 

        // this is broken
        // https://github.com/maartenba/MvcSiteMapProvider/issues/288
        //private static void BindSiteMapProvider(IKernel container)
        //{
        //    // Setup configuration of DI (required)
        //    container.Load(new MvcSiteMapProviderModule());
        //    // Setup global sitemap loader (required)
            
        //    MvcSiteMapProvider.SiteMaps.Loader = container.Get<ISiteMapLoader>();
        //    // Check all configured .sitemap files to ensure they follow the XSD for MvcSiteMapProvider (optional)
        //    //var validator = container.Get<ISiteMapXmlValidator>();
        //    //validator.ValidateXml(System.Web.Hosting.HostingEnvironment.MapPath("~/site-static.sitemap"));
        //    // Register the Sitemaps routes for search engines (optional)
        //    //XmlSiteMapController.RegisterRoutes(RouteTable.Routes);
            
        //}
    }
}
