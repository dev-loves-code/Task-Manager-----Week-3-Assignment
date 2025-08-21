using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Account;
using api.Interfaces;
using api.Models;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace api.Services
{
    public class ReportService : IReportService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ITaskService _taskService;
        private readonly INotificationService _notifications;
        private readonly ILogger<TaskService> _logger;

        public ReportService(UserManager<AppUser> userManager, ITaskService taskService, INotificationService notifications, ILogger<TaskService> logger)
        {
            _taskService = taskService;
            _userManager = userManager;
            _notifications = notifications;
            _logger = logger;
        }

        public async System.Threading.Tasks.Task GeneratePdf(UserDtoWithID user)
        {
            var pastDue = await _taskService.GetPastDueTasks(user.Id);
            var upcoming = await _taskService.GetUpcommingTasks(user.Id);

            Settings.License = LicenseType.Community;


            Settings.EnableDebugging = true;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header()
                        .Height(80)
                        .Background(Colors.Blue.Medium)
                        .AlignCenter()
                        .AlignMiddle()
                        .Text(text =>
                        {
                            text.DefaultTextStyle(x => x.FontColor(Colors.White));
                            text.AlignCenter();
                            text.Span("📋 Weekly Task Report").FontSize(24).Bold();
                            text.EmptyLine();
                            text.Span($"For: {user.Username}").FontSize(14);
                        });

                    page.Content()
                        .PaddingVertical(20)
                        .Column(column =>
                        {

                            column.Item().Column(col =>
                            {
                                col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(10).Row(row =>
                                {
                                    row.RelativeItem().Column(userCol =>
                                    {
                                        userCol.Item().Text($"👤 {user.Username ?? "Unknown User"}").FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                                        userCol.Item().Text($"📧 {user.Email ?? "No Email"}").FontSize(12).FontColor(Colors.Grey.Darken1);
                                    });
                                    row.ConstantItem(150).AlignRight().Text($"📅 {DateTime.Now:MMM dd, yyyy}").FontSize(12).FontColor(Colors.Grey.Darken1);
                                });
                                col.Item().PaddingTop(20);
                            });


                            column.Item().Column(pastDueCol =>
                            {
                                pastDueCol.Item().PaddingBottom(10).Row(row =>
                                {
                                    row.RelativeItem().Text("⚠️ Past Due Tasks (Previous Week)").FontSize(18).Bold().FontColor(Colors.Red.Medium);
                                    row.ConstantItem(80).AlignRight().Text($"({pastDue?.Count() ?? 0})").FontSize(14).FontColor(Colors.Red.Medium).Bold();
                                });

                                if (pastDue?.Any() == true)
                                {
                                    // Limit the number of tasks to prevent overflow
                                    var limitedPastDue = pastDue.Take(10);

                                    foreach (var task in limitedPastDue)
                                    {
                                        pastDueCol.Item().PaddingBottom(10).Border(1).BorderColor(Colors.Red.Lighten3).CornerRadius(8).Padding(12).Column(taskCol =>
                                        {
                                            taskCol.Item().Row(taskRow =>
                                            {
                                                taskRow.RelativeItem().Text(TruncateText(task.Title, 60)).FontSize(14).Bold().FontColor(Colors.Red.Darken1);
                                                taskRow.ConstantItem(100).AlignRight().Text($"Due: {task.DueDate:MMM dd}").FontSize(10).FontColor(Colors.Red.Medium);
                                            });

                                            if (!string.IsNullOrEmpty(task.Description))
                                            {
                                                taskCol.Item().PaddingTop(5).Text(TruncateText(task.Description, 150)).FontSize(11).FontColor(Colors.Grey.Darken2);
                                            }

                                            if (task.Notes?.Any() == true)
                                            {
                                                taskCol.Item().PaddingTop(8).Column(notesCol =>
                                                {
                                                    notesCol.Item().Text("📝 Notes:").FontSize(10).Bold().FontColor(Colors.Grey.Darken1);
                                                    foreach (var note in task.Notes.Take(2)) // Reduced to 2 notes
                                                    {
                                                        notesCol.Item().PaddingLeft(15).PaddingTop(3).Row(noteRow =>
                                                        {
                                                            noteRow.ConstantItem(8).AlignTop().Text("•").FontSize(8).FontColor(Colors.Grey.Medium);
                                                            noteRow.RelativeItem().PaddingLeft(5).Column(noteContent =>
                                                            {
                                                                noteContent.Item().Text(TruncateText(note.Title, 40)).FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                                                                if (!string.IsNullOrEmpty(note.Content))
                                                                {
                                                                    noteContent.Item().Text(TruncateText(note.Content, 80))
                                                                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                                                                }
                                                            });
                                                        });
                                                    }
                                                    if (task.Notes.Count > 2)
                                                    {
                                                        notesCol.Item().PaddingLeft(15).PaddingTop(3).Text($"... and {task.Notes.Count - 2} more notes")
                                                            .FontSize(8).Italic().FontColor(Colors.Grey.Medium);
                                                    }
                                                });
                                            }
                                        });
                                    }

                                    if (pastDue.Count() > 10)
                                    {
                                        pastDueCol.Item().PaddingTop(10).AlignCenter()
                                            .Text($"... and {pastDue.Count() - 10} more past due tasks")
                                            .FontSize(10).Italic().FontColor(Colors.Grey.Medium);
                                    }
                                }
                                else
                                {
                                    pastDueCol.Item().Padding(20).AlignCenter().Column(emptyCol =>
                                    {
                                        emptyCol.Item().Text("🎉").FontSize(32).AlignCenter();
                                        emptyCol.Item().PaddingTop(5).Text("Great job! No past due tasks from last week.")
                                            .FontSize(12).FontColor(Colors.Green.Medium).AlignCenter();
                                    });
                                }

                                pastDueCol.Item().PaddingTop(20);
                            });


                            column.Item().Column(upcomingCol =>
                            {
                                upcomingCol.Item().PaddingBottom(10).Row(row =>
                                {
                                    row.RelativeItem().Text("🚀 Upcoming Tasks (Next Week)").FontSize(18).Bold().FontColor(Colors.Green.Medium);
                                    row.ConstantItem(80).AlignRight().Text($"({upcoming?.Count() ?? 0})").FontSize(14).FontColor(Colors.Green.Medium).Bold();
                                });

                                if (upcoming?.Any() == true)
                                {
                                    // Limit the number of tasks to prevent overflow
                                    var limitedUpcoming = upcoming.OrderBy(t => t.DueDate).Take(10);

                                    foreach (var task in limitedUpcoming)
                                    {
                                        upcomingCol.Item().PaddingBottom(10).Border(1).BorderColor(Colors.Green.Lighten3).CornerRadius(8).Padding(12).Column(taskCol =>
                                        {
                                            taskCol.Item().Row(taskRow =>
                                            {
                                                taskRow.RelativeItem().Text(TruncateText(task.Title, 60)).FontSize(14).Bold().FontColor(Colors.Green.Darken1);
                                                taskRow.ConstantItem(100).AlignRight().Text($"Due: {task.DueDate:MMM dd}").FontSize(10).FontColor(Colors.Green.Medium);
                                            });

                                            if (!string.IsNullOrEmpty(task.Description))
                                            {
                                                taskCol.Item().PaddingTop(5).Text(TruncateText(task.Description, 150)).FontSize(11).FontColor(Colors.Grey.Darken2);
                                            }

                                            if (task.Notes?.Any() == true)
                                            {
                                                taskCol.Item().PaddingTop(8).Column(notesCol =>
                                                {
                                                    notesCol.Item().Text("📝 Notes:").FontSize(10).Bold().FontColor(Colors.Grey.Darken1);
                                                    foreach (var note in task.Notes.Take(2))
                                                    {
                                                        notesCol.Item().PaddingLeft(15).PaddingTop(3).Row(noteRow =>
                                                        {
                                                            noteRow.ConstantItem(8).AlignTop().Text("•").FontSize(8).FontColor(Colors.Grey.Medium);
                                                            noteRow.RelativeItem().PaddingLeft(5).Column(noteContent =>
                                                            {
                                                                noteContent.Item().Text(TruncateText(note.Title, 40)).FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                                                                if (!string.IsNullOrEmpty(note.Content))
                                                                {
                                                                    noteContent.Item().Text(TruncateText(note.Content, 80))
                                                                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                                                                }
                                                            });
                                                        });
                                                    }
                                                    if (task.Notes.Count > 2)
                                                    {
                                                        notesCol.Item().PaddingLeft(15).PaddingTop(3).Text($"... and {task.Notes.Count - 2} more notes")
                                                            .FontSize(8).Italic().FontColor(Colors.Grey.Medium);
                                                    }
                                                });
                                            }
                                        });
                                    }

                                    if (upcoming.Count() > 10)
                                    {
                                        upcomingCol.Item().PaddingTop(10).AlignCenter()
                                            .Text($"... and {upcoming.Count() - 10} more upcoming tasks")
                                            .FontSize(10).Italic().FontColor(Colors.Grey.Medium);
                                    }
                                }
                                else
                                {
                                    upcomingCol.Item().Padding(20).AlignCenter().Column(emptyCol =>
                                    {
                                        emptyCol.Item().Text("🏖️").FontSize(32).AlignCenter();
                                        emptyCol.Item().PaddingTop(5).Text("Looks like you have a light week ahead!")
                                            .FontSize(12).FontColor(Colors.Blue.Medium).AlignCenter();
                                    });
                                }
                            });
                        });

                    page.Footer()
                        .Height(40)
                        .Background(Colors.Grey.Lighten4)
                        .AlignCenter()
                        .AlignMiddle()
                        .Text(text =>
                        {
                            text.AlignCenter();
                            text.Span("Generated on ").FontSize(9).FontColor(Colors.Grey.Darken1);
                            text.Span($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                            text.Span(" | Stay organized, stay productive! 💪").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                });
            });

            try
            {

                var pdfBytes = document.GeneratePdf();


                var fileName = $"WeeklyReport_{user.Username}_{DateTime.Now:yyyyMMdd}.pdf";


                var tempPath = Path.Combine(Path.GetTempPath(), fileName);
                await File.WriteAllBytesAsync(tempPath, pdfBytes);

                await _notifications.SendPrivateMessageAsync(user.Id, "Your report is ready! Check your email");


                BackgroundJob.Enqueue<IEmailService>(x => x.SendReportEmailAsync(user.Email, user.Username, tempPath));
            }
            catch (Exception ex)
            {

                throw new InvalidOperationException($"Failed to generate PDF for user {user.Username}: {ex.Message}", ex);
            }
        }

        private static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text ?? string.Empty;

            return text.Substring(0, maxLength - 3) + "...";
        }

        public async System.Threading.Tasks.Task QueueWeeklyReports()
        {
            try
            {
                var allUsers = await _userManager.Users.Select(u => new UserDtoWithID
                {
                    Id = u.Id,
                    Email = u.Email,
                    Username = u.UserName
                }).ToListAsync();

                foreach (var user in allUsers)
                {
                    try
                    {
                        await GeneratePdf(user);
                    }
                    catch (Exception ex)
                    {

                        _logger.LogError("Failed to generate report for user {Username}: {Message}", user.Username, ex.Message);

                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to queue weekly reports: {ex.Message}", ex);
            }
        }
    }
}