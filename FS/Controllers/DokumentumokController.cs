using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DokumentumokController : ControllerBase
    {
        private readonly ILogger<DokumentumokController> _logger;
        private string _path;

        public static readonly string ErrorCfg = "Érvénytelen szerver konfiguráció!";
        public static readonly string ErrorName = "Érvénytelen FileName!";
        public static readonly string ErrorMiss = "Nincs ilyen FileName!";
        public static readonly string ErrorExists = "Ilyen nevű fájl már létezik!";

        public DokumentumokController(IConfiguration configuration, ILogger<DokumentumokController> logger)
        {
            _logger = logger;
            var section = configuration.GetSection("FS");
            _path = section.GetValue<string>("Directory", "").Replace("/", "\\").TrimEnd('\\');
        }

        [HttpGet]
        [Route("~/api/dokumentumok")]
        [Route("~/api/dokumentumok/{fileName}")]
        public async Task<IActionResult> Get(string fileName)
        {
            string errorMsg = null;
            string res = null;
            try
            {
                if (!Directory.Exists(_path))
                {
                    _logger.LogWarning("Érvénytelen útvonal: {0}", _path);
                    errorMsg = ErrorCfg;
                }
                else if (string.IsNullOrEmpty(fileName))
                {
                    DirectoryInfo dir = new DirectoryInfo(_path);
                    IEnumerable<FileInfo> files = dir.GetFiles("*.*", SearchOption.AllDirectories).OrderBy(f => f.FullName);
                    var list = files.Select(e => e.FullName.Substring(_path.Length + 1)).ToArray();
                    res = JsonSerializer.Serialize(list);
                }
                else
                {
                    string fullName = Path.Combine(_path, fileName.Trim().Replace("/", "\\").TrimStart('\\'));
                    if (!System.IO.File.Exists(fullName))
                    {
                        errorMsg = ErrorMiss;
                    }
                    else
                    {
                        var data = await System.IO.File.ReadAllBytesAsync(fullName);
                        res = Convert.ToBase64String(data);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FileService.Get");
                errorMsg = "Váratlan hiba!";
            }

            if (errorMsg != null)
            {
                return BadRequest(errorMsg);
            }

            return Ok(res);
        }

        [HttpPost]
        [Route("~/api/dokumentumok/{fileName}")]
        public async Task<IActionResult> Set(string fileName)
        {
            string errorMsg = null;
            try
            {
                if (!Directory.Exists(_path))
                {
                    _logger.LogWarning("Érvénytelen útvonal: {0}", _path);
                    errorMsg = ErrorCfg;
                }
                else
                {
                    fileName = fileName.Trim().Replace("/", "\\").TrimStart('\\');
                    string fullName = Path.Combine(_path, fileName);
                    string name = Path.GetFileName(fileName);
                    if (System.IO.File.Exists(fullName))
                    {
                        errorMsg = ErrorExists;
                    }
                    else if (Path.GetInvalidFileNameChars().Any(e => name.Contains(e)) ||
                        Path.GetInvalidPathChars().Any(e => fullName.Contains(e)) ||
                        fullName.IndexOf("..") != -1)
                    {
                        errorMsg = ErrorName;
                    }
                    else
                    {
                        var p = Path.GetDirectoryName(fullName);
                        if (!Directory.Exists(p))
                        {
                            Directory.CreateDirectory(p);
                        }
                        using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
                        {
                            string base64String = await reader.ReadToEndAsync();
                            var data = Convert.FromBase64String(base64String);
                            await System.IO.File.WriteAllBytesAsync(fullName, data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FileService.Set");
                errorMsg = "Váratlan hiba!";
            }

            if (errorMsg != null)
            {
                return BadRequest(errorMsg);
            }

            return Ok();
        }
    }
}
