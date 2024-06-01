using MedicielBack.controllers;
using MedicielBack.models;
using MedicielBack.services;
using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

public class Program
{
    public static void Main(string[] args)
    {
        var auditService = new AuditService();
        var adminController = new AdminController(auditService);
        var doctorService = new DoctorService(auditService);
        var doctorController = new DoctorController(auditService);
        var recordController = new MedicalRecordController(auditService, doctorService);


        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:5000/");
        listener.Start();
        Console.WriteLine("Listening on http://localhost:5000/");

        while (true)
        {
            var context = listener.GetContext();
            var request = context.Request;
            var response = context.Response;

            try
            {
                switch (request.Url.AbsolutePath.ToLower())
                {
                    case "/admin/register":
                        if (request.HttpMethod == "POST")
                        {
                            HandleRegisterAdmin(request, response, adminController);
                        }
                        break;

                    case "/admin/login":
                        if (request.HttpMethod == "POST")
                        {
                            HandleLoginAdmin(request, response, adminController);
                        }
                        break;

                    case "/admin/logout":
                        if (request.HttpMethod == "POST")
                        {
                            HandleLogoutAdmin(request, response, adminController);
                        }
                        break;

                    case "/admins":
                        if (request.HttpMethod == "GET")
                        {
                            HandleGetAllAdmins(request, response, adminController);
                        }
                        break;

                    case "/doctor/register":
                        if (request.HttpMethod == "POST")
                        {
                            HandleRegisterDoctor(request, response, adminController);
                        }
                        break;

                    case "/doctor/login":
                        if (request.HttpMethod == "POST")
                        {
                            HandleLoginDoctor(request, response, doctorController);
                        }
                        break;

                    case "/doctor/logout":
                        if (request.HttpMethod == "POST")
                        {
                            HandleLogoutDoctor(request, response, doctorController);
                        }
                        break;

                    case "/doctors":
                        if (request.HttpMethod == "GET")
                        {
                            HandleGetAllDoctors(request, response, doctorController);
                        }
                        break;

                    case "/records/create":
                        if (request.HttpMethod == "POST")
                        {
                            HandleCreateRecord(request, response, recordController);
                        }
                        break;

                    case "/records/update":
                        if (request.HttpMethod == "POST")
                        {
                            HandleUpdateRecord(request, response, recordController);
                        }
                        break;

                    case "/records":
                        if (request.HttpMethod == "GET")
                        {
                            HandleGetRecords(request, response, recordController);
                        }
                        break;

                    case "/record":
                        if (request.HttpMethod == "GET")
                        {
                            HandleGetRecord(request, response, recordController);
                        }
                        break;

                    case "/medicalrecords":
                        if (request.HttpMethod == "GET")
                        {
                            HandleGetAllMedicalRecords(request, response, recordController);
                        }
                        break;

                    default:
                        response.StatusCode = 404;
                        var notFoundMessage = Encoding.UTF8.GetBytes("Endpoint not found");
                        response.OutputStream.Write(notFoundMessage, 0, notFoundMessage.Length);
                        response.OutputStream.Close();
                        break;
                }
            }
            catch (Exception ex)
            {
                auditService.LogError($"Request handling failed. Exception: {ex.Message}");
                response.StatusCode = 500;
                var errorMessage = Encoding.UTF8.GetBytes("Internal server error");
                response.OutputStream.Write(errorMessage, 0, errorMessage.Length);
                response.OutputStream.Close();
            }
        }
    }

    private static void HandleRegisterAdmin(HttpListenerRequest request, HttpListenerResponse response, AdminController adminController)
    {
        string body;
        using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
        {
            body = reader.ReadToEnd();
        }

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(body);
        var username = data["username"];
        var password = data["password"];

        var admin = adminController.RegisterAdmin(username, password);
        var responseString = JsonSerializer.Serialize(admin);

        if (admin != null)
        {
            response.StatusCode = 201;
            responseString = JsonSerializer.Serialize(new { message = "Admin registered successfully." });
        }
        else
        {
            response.StatusCode = 400;
            responseString = JsonSerializer.Serialize(new { error = "Registration failed." });
        }

        var buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    private static void HandleLoginAdmin(HttpListenerRequest request, HttpListenerResponse response, AdminController adminController)
    {
        string body;
        using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
        {
            body = reader.ReadToEnd();
        }

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(body);
        var username = data["username"];
        var password = data["password"];

        var admin = adminController.LoginAdmin(username, password);
        var responseString = JsonSerializer.Serialize(admin);

        if (admin != null)
        {
            response.StatusCode = 200;
            responseString = JsonSerializer.Serialize(new { message = "Admin logged in successfully.", admin });
        }
        else
        {
            response.StatusCode = 400;
            responseString = JsonSerializer.Serialize(new { error = "Invalid username or password." });
        }

        var buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    private static void HandleLogoutAdmin(HttpListenerRequest request, HttpListenerResponse response, AdminController adminController)
    {
        string body;
        using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
        {
            body = reader.ReadToEnd();
        }

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(body);
        var token = data["token"];

        var admin = adminController.GetAdminByToken(token);

        if (admin != null)
        {
            adminController.LogoutAdmin(token);
            response.StatusCode = 200;
            var responseString = JsonSerializer.Serialize(new { message = "Admin logged out." });
            var buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
        else
        {
            response.StatusCode = 400;
            var responseString = JsonSerializer.Serialize(new { error = "Invalid token." });
            var buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }

    private static void HandleGetAllAdmins(HttpListenerRequest request, HttpListenerResponse response, AdminController adminController)
    {
        var admins = adminController.GetAllAdmins();
        var responseString = JsonSerializer.Serialize(admins);

        response.StatusCode = 200;
        var buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    private static void HandleRegisterDoctor(HttpListenerRequest request, HttpListenerResponse response, AdminController adminController)
    {
        string body;
        using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
        {
            body = reader.ReadToEnd();
        }

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(body);
        var token = data["token"];
        var matricule = data["matricule"];
        var password = data["password"];

        var doctor = adminController.RegisterDoctor(token, matricule, password);
        var responseString = JsonSerializer.Serialize(doctor);

        if (doctor != null)
        {
            response.StatusCode = 201;
            responseString = JsonSerializer.Serialize(new { message = "Doctor registered successfully." });
        }
        else
        {
            response.StatusCode = 400;
            responseString = JsonSerializer.Serialize(new { error = "Doctor registration failed." });
        }

        var buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    private static void HandleLoginDoctor(HttpListenerRequest request, HttpListenerResponse response, DoctorController doctorController)
    {
        string body;
        using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
        {
            body = reader.ReadToEnd();
        }

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(body);
        var matricule = data["matricule"];
        var password = data["password"];

        var doctor = doctorController.Login(matricule, password);
        var responseString = JsonSerializer.Serialize(doctor);

        if (doctor != null)
        {
            response.StatusCode = 200;
            responseString = JsonSerializer.Serialize(new { message = "Doctor logged in successfully.", doctor });
        }
        else
        {
            response.StatusCode = 400;
            responseString = JsonSerializer.Serialize(new { error = "Invalid matricule or password." });
        }

        var buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    private static void HandleLogoutDoctor(HttpListenerRequest request, HttpListenerResponse response, DoctorController doctorController)
    {
        string body;
        using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
        {
            body = reader.ReadToEnd();
        }

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(body);
        var token = data["token"];

        var doctor = doctorController.GetDoctorByToken(token);

        if (doctor != null)
        {
            doctorController.Logout(doctor);
            response.StatusCode = 200;
            var responseString = JsonSerializer.Serialize(new { message = "Doctor logged out." });
            var buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
        else
        {
            response.StatusCode = 400;
            var responseString = JsonSerializer.Serialize(new { error = "Invalid token." });
            var buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }

    private static void HandleGetAllDoctors(HttpListenerRequest request, HttpListenerResponse response, DoctorController doctorController)
    {
        var doctors = doctorController.GetAllDoctors();
        var responseString = JsonSerializer.Serialize(doctors);

        response.StatusCode = 200;
        var buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    private static void HandleCreateRecord(HttpListenerRequest request, HttpListenerResponse response, MedicalRecordController recordController)
    {
        string body;
        using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
        {
            body = reader.ReadToEnd();
        }

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(body);
        var token = data["token"];
        var patientName = data["patientName"];
        var diagnosis = data["diagnosis"];
        var treatment = data["treatment"];

        var record = recordController.CreateRecord(token, patientName, diagnosis, treatment);
        var responseString = JsonSerializer.Serialize(record);

        if (record != null)
        {
            response.StatusCode = 201;
            responseString = JsonSerializer.Serialize(new { message = "Record created successfully.", record });
        }
        else
        {
            response.StatusCode = 400;
            responseString = JsonSerializer.Serialize(new { error = "Record creation failed." });
        }

        var buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    private static void HandleUpdateRecord(HttpListenerRequest request, HttpListenerResponse response, MedicalRecordController recordController)
    {
        string body;
        using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
        {
            body = reader.ReadToEnd();
        }

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(body);
        var token = data["token"];
        var recordId = int.Parse(data["recordId"]);
        var diagnosis = data["diagnosis"];
        var treatment = data["treatment"];

        var record = recordController.UpdateRecord(token, recordId, diagnosis, treatment);
        var responseString = JsonSerializer.Serialize(record);

        if (record != null)
        {
            response.StatusCode = 200;
            responseString = JsonSerializer.Serialize(new { message = "Record updated successfully.", record });
        }
        else
        {
            response.StatusCode = 400;
            responseString = JsonSerializer.Serialize(new { error = "Record update failed." });
        }

        var buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    private static void HandleGetRecords(HttpListenerRequest request, HttpListenerResponse response, MedicalRecordController recordController)
    {
        var token = request.QueryString["token"];

        var records = recordController.GetRecords(token);
        var responseString = JsonSerializer.Serialize(records);

        response.StatusCode = 200;
        var buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    private static void HandleGetRecord(HttpListenerRequest request, HttpListenerResponse response, MedicalRecordController recordController)
    {
        var token = request.QueryString["token"];
        var recordId = int.Parse(request.QueryString["recordId"]);

        var record = recordController.GetRecord(token, recordId);
        var responseString = JsonSerializer.Serialize(record);

        if (record != null)
        {
            response.StatusCode = 200;
        }
        else
        {
            response.StatusCode = 400;
            responseString = JsonSerializer.Serialize(new { error = "Record not found." });
        }

        var buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    private static void HandleGetAllMedicalRecords(HttpListenerRequest request, HttpListenerResponse response, MedicalRecordController recordController)
    {
        var records = recordController.GetAllMedicalRecords();
        var responseString = JsonSerializer.Serialize(records);

        response.StatusCode = 200;
        var buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }
}
