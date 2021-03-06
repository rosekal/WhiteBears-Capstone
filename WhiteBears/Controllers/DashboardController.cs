﻿using WhiteBears.Models;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WhiteBears;

namespace WhiteBears.Controllers {
    public class DashboardController : Controller {

        Project[] projects;
        User currUser;
        public static SqlDataAdapter da;

        public ActionResult Index() {
            DataRow[] drs;

            DashboardModel model = new DashboardModel();
            string username;

            if(Session["username"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            username = Session["username"].ToString();
            
            if (Authentication.VerifyIfAdmin(username)){
                return RedirectToAction("Index", "AdminPage");
            }

            drs = model.GetUser(Session["username"].ToString());

            //TODO: refactor the email to dr["email"]
            currUser = new User(drs[0]["firstName"].ToString(),
                drs[0]["lastName"].ToString(),
                drs[0]["uName"].ToString(),
                drs[0]["firstName"].ToString() + "@email.com",
                drs[0]["password"].ToString(),
                drs[0]["role"].ToString()) {
            };

            currUser.PersonalNotes = GetPersonalNotes(model);
            model.Projects = GetAllProjects(currUser.Username, model);
            model.CurrentUser = currUser;
            model.CurrDate = DateTime.Now;

            return View(model);
        }

        private Project[] GetAllProjects(string username, DashboardModel model) {
            DataRow[] drs, drs1 = null;
            drs = model.GetProjects(username);
            

            projects = new Project[drs.Count()];

            int i = 0;
            foreach (DataRow dr in drs) {
                int currProjectId = Int32.Parse(drs[i]["projectId"].ToString());
                string projectTitle = dr["title"].ToString();

                if (Authentication.VerifyIfProjectManager(username)) {
                    drs1 = model.GetAllTasks(currProjectId);
                } else {
                    drs1 = model.GetTasks(username, currProjectId);
                }

                List<Task> currProjectTasks = new List<Task>();

                foreach (DataRow dr1 in drs1) {
                    DateTime dueDate = Convert.ToDateTime(dr1["dueDate"]);
                    DateTime completionDate = Convert.ToDateTime(dr1["completionDate"]);

                    string completionDateString = completionDate.ToString("MM/dd/yyyy");
                    if (!completionDateString.Equals("01/01/0001") && !completionDateString.Equals("01-01-0001"))
                        continue;

                    currProjectTasks.Add(new Task {
                        TaskId = Convert.ToInt32(dr1["taskId"]),
                        Title = dr1["title"].ToString(),
                        Priority = dr1["priority"].ToString(),
                        DueDate = dueDate,
                        Status = DateTime.Now < dueDate ? "On time" : "Overdue",
                        ProjectName = projectTitle,
                        CompletedDate = completionDate,
                        Users = model.GetTaskUsers(Convert.ToInt32(dr1["taskId"]))
                    });
                }

                projects[i++] = new Project {
                    ProjectId = Int32.Parse(dr["projectId"].ToString()),
                    Title = projectTitle,
                    Tasks = currProjectTasks.ToArray()
                };
            }

            return projects;
        }

        private PersonalNote[] GetPersonalNotes(DashboardModel model) {
            DataRow[] drs = model.GetPersonalNote(currUser.Username);
            PersonalNote[] notes = new PersonalNote[drs.Count()];
            int i = 0;
            foreach (DataRow dr in drs) {
                notes[i++] = new PersonalNote(Int32.Parse(dr["noteId"].ToString()), dr["note"].ToString(), Convert.ToDateTime(dr["date"]));
            }

            return notes;
        }

        [HttpPost]
        public ActionResult UpdateProject(string projectId, string sUser) {
            List<Task> selectedTasks = new List<Task>();
            DashboardModel model = new DashboardModel();
            DataRow[] drs;

            bool isProjectManager = false;

            if (projectId == "_all") {
                isProjectManager = Authentication.VerifyIfProjectManager(sUser);
                Project[] projects = GetAllProjects(sUser, model);
                model.Projects = projects;
                foreach (Project p in projects) {
                    foreach (Task t in p.Tasks) {
                        selectedTasks.Add(t);
                    }
                }

            } else {
                int iProjectId = Convert.ToInt32(projectId);
                model = new DashboardModel();


                isProjectManager = Authentication.VerifyIfProjectManager(sUser);
                if (isProjectManager) {
                    drs = model.GetAllTasks(iProjectId);
                } else {
                    drs = model.GetTasks(sUser, iProjectId);
                }

                string projectName = model.GetProjectName(projectId);

                foreach (DataRow dr in drs) {
                    DateTime dueDate = Convert.ToDateTime(dr["dueDate"]);
                    DateTime completionDate = Convert.ToDateTime(dr["completionDate"]);

                    string completionDateString = completionDate.ToString("MM/dd/yyyy");
                    if (!completionDateString.Equals("01/01/0001") && !completionDateString.Equals("01-01-0001"))
                        continue;


                    selectedTasks.Add(new Task {
                        Title = dr["title"].ToString(),
                        Priority = dr["priority"].ToString(),
                        DueDate = dueDate,
                        Status = DateTime.Now < dueDate ? "On time" : "Overdue",
                        ProjectName = projectName,
                        CompletedDate = completionDate,
                        Users = model.GetTaskUsers(Convert.ToInt32(dr["taskId"]))
                });
                }

                model.Projects = new Project[]{
                    new Project{
                        Tasks = selectedTasks.ToArray()
                    }
                };
            }

            var users = new System.Text.StringBuilder();

            


            var sb = new System.Text.StringBuilder();
            foreach (Task task in selectedTasks) {
                users.Clear();
                users.Append("");
                if (isProjectManager) {
                    foreach (User u in task.Users) {
                        if(task.Users.Last() != u) {
                            users.Append($"{u.FirstName} {u.LastName}, ");
                        } else {
                            users.Append($"{u.FirstName} {u.LastName}");
                        }
                        
                    }
                }
                sb.Append($"<tr>" +
                    $"<td>{task.Title}</td>" +
                    $"<td>{task.Priority}</td>" +
                    $"<td>{task.DueDate.ToString("MM/dd/yyyy")}</td>" +
                    $"<td>{task.Status}</td>" +
                    $"<td>{task.ProjectName}</td>" +
                    (isProjectManager ? $"<td>{users.ToString()}</tb>" : "") +
                    $"</tr>");
            }

            return Json(new { row = sb.ToString() });
        }


        [HttpPost]
        public ActionResult AddNote(string input, string username) {
            DashboardModel model = new DashboardModel();

            input = input.Replace("'", "''");
            DataRow[] drs = model.AddPersonalNote(username, input);

            return Json(new { success = true, message = drs[0][0].ToString() });
        }

        [HttpPost]
        public ActionResult DeleteNote(string noteId) {
            DashboardModel model = new DashboardModel();

            string results = "" + model.DeletePersonalNote(Int32.Parse(noteId));
            return Json(new { success = true, data = noteId });
        }

        [HttpPost]
        public ActionResult UpdateNote(string input, string noteId) {
            DashboardModel model = new DashboardModel();

            input = input.Replace("'", "''");
            string results = "" + model.UpdatePersonalNote(input, Int32.Parse(noteId));
            return Json(new { success = true, message = noteId });
        }

        [HttpPost]
        public ActionResult LoadCompletedTasks(string projectId, string username) {
            DashboardModel model = new DashboardModel();
            Project p = null;

            bool isProjectManager = Authentication.VerifyIfProjectManager(username);

            if (projectId.Equals("_all")) {
                List<Task> tasks = new List<Task>();

                DataRow[] drs = model.GetProjects(username);
                int i = 0;
                foreach (DataRow dr in drs) {
                    int currProjectId = Int32.Parse(drs[i++]["projectId"].ToString());
                    string projectTitle = dr["title"].ToString();

                    DataRow[] drs1 = null;
                    if (isProjectManager) {
                        drs1 = model.GetAllTasks(currProjectId);
                    } else {
                        drs1 = model.GetTasks(username, currProjectId);
                    }

                    foreach (DataRow dr1 in drs1) {
                        DateTime dueDate = Convert.ToDateTime(dr1["dueDate"]);
                        DateTime completionDate = Convert.ToDateTime(dr1["completionDate"]);

                        tasks.Add(new Task {
                            Title = dr1["title"].ToString(),
                            Priority = dr1["priority"].ToString(),
                            DueDate = dueDate,
                            Status = DateTime.Now < dueDate ? "On time" : "Overdue",
                            ProjectName = projectTitle,
                            CompletedDate = completionDate,
                        });
                    }
                }
                p = new Project {
                    Tasks = tasks.ToArray()
                };
            } else {
                int iProjectId = Convert.ToInt32(projectId);

                List<Task> tasks = new List<Task>();
                if (!isProjectManager)
                    p = model.GetProject(username, projectId);
                else {
                    p = model.GetProject(username, projectId);

                    DataRow[] drs1 = model.GetAllTasks(iProjectId);

                    foreach (DataRow dr1 in drs1) {
                        DateTime dueDate = Convert.ToDateTime(dr1["dueDate"]);
                        DateTime completionDate = Convert.ToDateTime(dr1["completionDate"]);

                        tasks.Add(new Task {
                            Title = dr1["title"].ToString(),
                            Priority = dr1["priority"].ToString(),
                            DueDate = dueDate,
                            Status = DateTime.Now < dueDate ? "On time" : "Overdue",
                            ProjectName = p.Title,
                            CompletedDate = completionDate,
                        });
                    }

                    p.Tasks = tasks.ToArray();
                }
            }


            return Json(new { success = true, message = p.Tasks });
        }
    }
}
