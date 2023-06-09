﻿using Microsoft.AspNetCore.Mvc;
using TeamProject.Data;
using TeamProject.Models.Domain;
using TeamProject.Models.ViewModels;
using TeamProject.Repositories;
using ActionResult = Microsoft.AspNetCore.Mvc.ActionResult;
using Controller = Microsoft.AspNetCore.Mvc.Controller;

namespace TeamProject.Controllers
{
    //
    public class LoginController : Controller
    {
       
        private readonly IUserRepository userRepository;
        
        
        public LoginController(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }

        [HttpGet]
        public IActionResult Login(LoginRequest loginRequest)
        {
            return View(loginRequest);
        }


        [HttpGet]
        //회원가입 초기 세팅
        public IActionResult SignUp(AddUserRequest addUserRequest)
        {
            addUserRequest = new()
            {
                Name = (string)TempData["Name"],
                NameCheck = (bool?)TempData["NameCheck"],
                PassWord = (string)TempData["PassWord"],
                PassWordCheck = (string)TempData["PassWordCheck"],
                UserName =(string)TempData["UserName"],
                PhoneNum = (string)TempData["PhoneNum"],
                NickName = (string)TempData["NickName"],
                Email = (string)TempData["Email"],
            };            
            return View(addUserRequest);
        }


        [HttpGet("CheckId")]
        public async Task<IActionResult> CheckId(string id)
        {           
            var isExsist = await userRepository.GetByNameAsync(id);
            if (isExsist == null)
            {
               
                return Ok(true);
            }
            return Ok(false) ;
        }


        [HttpPost]
        [ActionName("SignUp")]
        public async Task<IActionResult> Add(AddUserRequest addUserRequest)
        {
            addUserRequest.Validate();
            if (addUserRequest.HasError)
            {
                TempData["NameError"] = addUserRequest.ErrorName;
                TempData["PassWordError"] = addUserRequest.ErrorPassWord;
                TempData["UserNameError"] = addUserRequest.ErrorUserName;
                TempData["PhoneNumError"] = addUserRequest.ErrorPhoneNum;
                TempData["NickNameError"] = addUserRequest.ErrorNickName;
                TempData["EmailError"] = addUserRequest.ErrorEmail;

                TempData["Name"] = addUserRequest.Name;
                TempData["NameCheck"] = addUserRequest.NameCheck;
                TempData["PassWord"] = addUserRequest.PassWord;
                TempData["PassWordCheck"] = addUserRequest.PassWordCheck;
                TempData["UserName"] = addUserRequest.UserName;
                TempData["PhoneNum"] = addUserRequest.PhoneNum;
                TempData["NickName"] = addUserRequest.NickName;
                TempData["Email"] = addUserRequest.Email;

                return RedirectToAction("SignUp");
            }

            var user = new User
            {
                Name = addUserRequest.Name,
                PassWord = addUserRequest.PassWord,
                UserName = addUserRequest.UserName,
                PhoneNum = addUserRequest.PhoneNum,
                NickName = addUserRequest.NickName,
                Email = addUserRequest.Email,                

            };

            user = await userRepository.AddAsync(user);
            return RedirectToAction("Login");
        }


        [HttpPost]
        public async Task<IActionResult> Login(string name, string password)
        {
			User existingUser = await userRepository.GetByNameAsync(name);
            if (name == null)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                TempData["NullId"] = "Invalid username or password.";
                return View();
            }
            else if (password == null)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                TempData["NullPw"] = "Invalid username or password.";
                return View();

            }
			if (existingUser == null)
			{
                // 사용자가 존재하지 않을 경우 로그인 실패 처리를 합니다.
                ModelState.AddModelError("", "Invalid username or password.");
                TempData["ErrorMessageId"] = "Invalid username or password.";
                return View();
			}

			bool isPasswordValid = existingUser.PassWord == password;
			if (!isPasswordValid)
			{
                // 비밀번호가 일치하지 않을 경우 로그인 실패 처리를 합니다.
                ModelState.AddModelError("", "Invalid username or password.");
                TempData["ErrorMessagePw"] = "Invalid username or password.";
                return View();
			}

			HttpContext.Session.SetString("UserId", existingUser.Id.ToString());
            HttpContext.Session.SetString("IsLoggedIn", "true");
            HttpContext.Session.SetString("IsAdmin",existingUser.IsAdmin.ToString());

            // 로그인 성공 후 리다이렉트할 페이지를 지정합니다.
            return RedirectToAction("Index", "Home");
		}

        [HttpPost]
		public async Task<IActionResult> Logout()
		{
			// 세션에서 사용자 정보 및 로그인 상태를 삭제합니다.
			HttpContext.Session.Remove("UserId");
			HttpContext.Session.Remove("IsLoggedIn");
			HttpContext.Session.Remove("IsAdmin");

			// 로그아웃 후 리다이렉트할 페이지를 지정합니다. (예: 로그인 페이지, 홈 페이지 등)
			return RedirectToAction("Index", "Home");
		}
        [HttpGet]
        public IActionResult FindID()
        {
            return View();
        }
        [HttpGet("FindUserId")]
        public async Task<IActionResult> FindUserId(string name, string phone)
        {
            var isExsist = await userRepository.GetByUserNameAsync(name);

            string status = "";
            if (isExsist == null)
            {

                return Ok(status);
            }
            else if (isExsist != null)
            {
                if (isExsist.PhoneNum == phone)
                {
                    status = isExsist.Name;
                    return Ok(status);
                }
                else if (isExsist.PhoneNum != phone)
                {
                    status = "1";
                    return Ok(status);
                }
            }

            return Ok(status);
        }
        [HttpGet("FindUserPw")]
        public async Task<IActionResult> FindUserPw(string id, string phone)
        {
            var isExsist = await userRepository.GetByNameAsync(id);

            string status = "";
            if (isExsist == null)
            {

                return Ok(status);
            }
            else if (isExsist != null)
            {
                if (isExsist.PhoneNum == phone)
                {
                    status = "2";
                    return Ok(status);
                }
                else if (isExsist.PhoneNum != phone)
                {
                    status = "1";
                    return Ok(status);
                }
            }

            return Ok(status);
        }

        [HttpGet("PasswordUpdate")]
        public async Task<IActionResult> PasswordUpdate(string pw,string id)
        {
            var user = await userRepository.GetByNameAsync(id);
            var upUser = new User
            {
                Id = user.Id,
                PassWord = pw,
            };
            var upPass = await userRepository.UpdatPassAsync(upUser);
            if (upPass == null)
            {
                // 수정 실패하면 List 로
                return Ok(false);
            }
            return Ok(true);

            
        }
    }
}
