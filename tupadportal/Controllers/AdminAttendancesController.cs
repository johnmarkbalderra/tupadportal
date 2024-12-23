using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tupadportal.Data;
using tupadportal.Models;
using Microsoft.AspNetCore.Authorization;
using tupadportal.Extensions; // Include the extensions namespace
using tupadportal.ViewModels; // Include this for ApplicantAttendanceViewModel
using SelectPdf;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding; // Assuming you use SelectPdf or similar for PDF generation


namespace tupadportal.Controllers
{
    [Authorize]
    [Route("AdminAttendances")]
    public class AdminAttendancesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminAttendancesController> _logger;

        public AdminAttendancesController(ApplicationDbContext context, ILogger<AdminAttendancesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var checklists = await _context.AttendanceChecklists
                                           .Include(c => c.Applicant)
                                           .ToListAsync();
            return View(checklists);
        }

        [HttpGet("ApplicantAttendance/{applicantId}")]
        public async Task<IActionResult> AdminApplicantAttendance(int applicantId)
        {
            var applicant = await _context.Applicants.FindAsync(applicantId);

            if (applicant == null)
            {
                return NotFound("Applicant not found.");
            }

            var attendances = await _context.Attendances
                                            .Where(a => a.ApplicantId == applicantId)
                                            .ToListAsync();

            var viewModel = new ApplicantAttendanceViewModel
            {
                ApplicantId = applicantId,
                FirstName = applicant.FirstName,
                LastName = applicant.LastName,
                Barangay = applicant.Barangay,
                Attendances = attendances
            };

            if (Request.IsAjaxRequest())
            {
                return PartialView("_AdminApplicantAttendance", viewModel);
            }

            return View(viewModel);
        }


        //[HttpGet("SaveAsPdf")]
        [Route("AdminAttendances/SaveAsPdf")]
        public async Task<IActionResult> SaveAsPdf(int applicantId)
        {
            var applicant = await _context.Applicants.FindAsync(applicantId);
            if (applicant == null)
            {
                return NotFound("Applicant not found.");
            }

            var attendances = await _context.Attendances
                                            .Where(a => a.ApplicantId == applicantId)
                                            .ToListAsync();

            // Render HTML to string for PDF generation
            var htmlContent = RenderToString("_AttendancePdf", new ApplicantAttendanceViewModel
            {
                ApplicantId = applicantId,
                FirstName = applicant.FirstName,
                LastName = applicant.LastName,
                Barangay = applicant.Barangay,
                Attendances = attendances
            });

            // Generate PDF
            try
            {
                var pdf = new HtmlToPdf();
                PdfDocument pdfDocument = pdf.ConvertHtmlString(await htmlContent.ConfigureAwait(false));
                var pdfBytes = pdfDocument.Save();
                pdfDocument.Close();

                // Return the generated PDF as a file
                return File(pdfBytes, "application/pdf", "Attendance.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF.");
                return StatusCode(500, "Internal server error while generating PDF.");
            }
        }

        private async Task<string> RenderToString(string viewName, object model)
        {
            var controllerContext = this.ControllerContext;
            var viewEngine = controllerContext.HttpContext.RequestServices.GetService<ICompositeViewEngine>();
            var tempDataProvider = controllerContext.HttpContext.RequestServices.GetService<ITempDataProvider>();

            var viewResult = viewEngine.FindView(controllerContext, viewName, false);
            if (viewResult.Success == false)
            {
                throw new InvalidOperationException($"Could not find view {viewName}");
            }

            using (var stringWriter = new StringWriter())
            {
                var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = model
                };

                var viewContext = new ViewContext(
                    controllerContext,
                    viewResult.View,
                    viewData,
                    new TempDataDictionary(controllerContext.HttpContext, tempDataProvider),
                    stringWriter,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return stringWriter.ToString(); // This is the generated HTML content
            }
        }

        // Other actions...
    }
}
