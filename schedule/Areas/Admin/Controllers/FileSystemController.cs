using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using elFinder.NetCore;
using elFinder.NetCore.Drivers.FileSystem;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using it.Areas.Admin.Models;

namespace it.Areas.Admin.Controllers
{
    [Area("Admin")]
    // [Authorize] - bỏ comment user phải đăng nhập mới dùng được
    public class FileSystemController : Controller
    {
        private UserManager<UserModel> UserManager;
        IWebHostEnvironment _env;
        public FileSystemController(IWebHostEnvironment env, UserManager<UserModel> UserMgr)
        {
            _env = env; UserManager = UserMgr;
        }

        // Url để client-side kết nối đến backend
        // /el-finder-file-system/connector
        public async Task<IActionResult> Connector()
        {
            var connector = GetConnector();
            return await connector.ProcessAsync(Request);
        }

        // Địa chỉ để truy vấn thumbnail
        // /el-finder-file-system/thumb
        public async Task<IActionResult> Thumbs(string hash)
        {
            var connector = GetConnector();
            return await connector.GetThumbnailAsync(HttpContext.Request, HttpContext.Response, hash);
        }

        private Connector GetConnector()
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:

            // Thư mục gốc lưu trữ là wprivate/files (đảm bảo có tạo thư mục này)
            string pathroot = "private\\upload\\" + user_id;
            string pathrooturl = "private/upload/" + user_id;
            bool exists = System.IO.Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), pathroot));

            if (!exists)
                System.IO.Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), pathroot));
            var driver = new FileSystemDriver();

            string absoluteUrl = UriHelper.BuildAbsolute(Request.Scheme, Request.Host);
            var uri = new Uri(absoluteUrl);

            // .. ... wwww/files
            string rootDirectory = Path.Combine(Directory.GetCurrentDirectory(), pathroot);
            //return Ok(rootDirectory);
            // https://localhost:5001/files/
            string url = $"{uri.Scheme}://{uri.Authority}/{pathrooturl}/";
            string urlthumb = $"{uri.Scheme}://{uri.Authority}/admin/FileSystem/thumbs/";


            var root = new RootVolume(rootDirectory, url, urlthumb)
            {
                //IsReadOnly = !User.IsInRole("Administrators")
                IsReadOnly = false, // Can be readonly according to user's membership permission
                IsLocked = false, // If locked, files and directories cannot be deleted, renamed or moved
                Alias = "/" + pathrooturl, // Beautiful name given to the root/home folder
                MaxUploadSizeInMb = 10, // Limit imposed to user uploaded file <= 10 MB
                //LockedFolders = new Lis   t<string>(new string[] { "Folder1" }
                ThumbnailSize = 100,
            };


            driver.AddRoot(root);

            return new Connector(driver)
            {
                // This allows support for the "onlyMimes" option on the client.
                MimeDetect = MimeDetectOption.Internal
            };
        }
    }
}
