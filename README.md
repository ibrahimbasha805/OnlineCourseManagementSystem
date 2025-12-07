# Online Course Management System

An enterprise-ready **Online Course Management System** built using **.NET 8 Web API** following **Clean Architecture** principles.  
This system manages **Users, Courses, Enrollment, Authentication, Authorization, and Role-based access control** in a microservice-ready structure.

---

## 🚀 Features

✅ User Registration & Login (JWT Authentication)  
✅ Role-based Authorization (Instructor, Student)  
✅ Course Creation & Management  
✅ Student Enrollment into Courses  
✅ Secure Password Hashing (BCrypt)  
✅ API-to-API Communication using HttpClient  
✅ Centralized Exception Handling with ProblemDetails  
✅ Clean Architecture (Domain, Application, Infrastructure, API)  
✅ Dockerized Deployment  
✅ nginx Reverse Proxy Setup  
✅ Scalable Microservice-Ready Structure  

---

## 🛠 Tech Stack

- **Backend:** .NET 8 Web API  
- **Language:** C#  
- **Authentication:** JWT Bearer Token  
- **Password Security:** BCrypt.Net  
- **Architecture:** Clean Architecture  
- **Logging:** Serilog 
- **Containers:** Docker, Docker Compose  
- **Reverse Proxy:** Nginx  
- **API Communication:** HttpClient 
- **Database:** In-Memory (EF Core)  

---


## 📂 Project Structure

```
OnlineCourseManagementSystem/
│
├── CourseService/                        # Course Microservice (.NET 8)
│   ├── CourseService.API                 # API Layer
│   ├── CourseService.Application         # Application Layer (Business Logic)
│   ├── CourseService.Domain              # Domain Layer (Core Business Models)
│   ├── CourseService.Infrastructure      # Infrastructure Layer (Database & External APIs)
│   └── CourseService.Tests               # Unit & Integration Tests
│
├── UserService/                           # User Microservice (.NET 8)
│   ├── UserService.API                   # API Layer
│   ├── UserService.Application           # Application Layer
│   ├── UserService.Domain                # Domain Layer
│   ├── UserService.Infrastructure        # Infrastructure Layer
│   └── UserService.Tests                 # Unit & Integration Tests
│
├── certs/                                # SSL Certificates (optional)
├── nginx_conf/                           # Nginx Reverse Proxy Configuration
└── README.md                             # Project Documentation

```

---

## ⚙️ Prerequisites

Before running this project, install:

- ✅ .NET SDK 8  
- ✅ Docker Desktop  
- ✅ SQL Server  
- ✅ Git  
- ✅ Visual Studio 2022  

---
## 🔐 Authentication Flow

1. User registers with **UserName** and **password**
2. Password is securely hashed using **BCrypt**
3. User logs in → a **JWT token** is generated
4. The token is sent in the `Authorization` HTTP header as:
   ```http
   Authorization: Bearer <token>
| Role       | Permissions                              |
| ---------- | ---------------------------------------- |   
| Instructor | Create & manage courses, enroll students |
| Student    | View & enroll in assigned courses        |

---
## 📡 API Communication

### ✅ UserService Handles
- Registration  
- Login  
- Enroll Course  

### ✅ CourseService Handles
- Course creation  
- Enrollment  
- Course listing  

### 🔗 Service-to-Service Communication
- Course enrollment is triggered via:
  - **UserService → CourseService** (using **HttpClient**)

- All failures between services are returned as:
  - **ProblemDetails based responses (RFC 7807 compliant)**
  ---
  ## 🛑 Error Handling

The system uses **centralized and standardized error handling** for all APIs:

- Global **Exception Handling Middleware**
- Validation Errors (e.g., **FluentValidation** failures)
- Authentication Errors → **401 Unauthorized**
- Authorization Errors → **403 Forbidden**
- Business rule violations (**BadRequest**, etc.)

All of these return a structured **ProblemDetails** response, such as:

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Bad Request",
  "status": 400,
  "detail": "Validation failed for one or more fields.",
  "errors": {
    "Email": [ "UserName is required." ]
  }
}
```
---

## 📦 Docker & Nginx

- Each microservice runs in its **own Docker container**
- **Nginx** acts as a **reverse proxy** and single entry point to the system

### 🌐 Example Routes

```
http://localhost/users   → UserService
http://localhost/courses → CourseService
```

## Commands to run using Docker
---

- **Create a custom Docker network** : docker network create ocms-net

- **Create course service docker Image (using from local path)** :
docker build -f Docker\Dockerfile -t ocms/courseservice:1.0 .

- **Create user service docker Image (using from local path)** :
docker build -f Docker\Dockerfile -t ocms/userservice:1.0 .

- **Create user service docker Container** :
docker run -d --name userservice --network ocms-net --env-file <localpath>\users.env -p 5000:5000 --restart unless-stopped ocms/userservice:1.0

- **Create Course service docker Container** :
docker run -d --name courseservice --network ocms-net --env-file <localpath>\courses.env -p 5001:5001 --restart unless-stopped ocms/courseservice:1.0
