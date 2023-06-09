﻿using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using MimeMapping;
using System.ComponentModel;
using TeamProject.Data;
using TeamProject.Models.Domain;
using TeamProject.Models.ViewModels;
using TeamProject.Repositories;
using ActionResult = Microsoft.AspNetCore.Mvc.ActionResult;
using Controller = Microsoft.AspNetCore.Mvc.Controller;


namespace TeamProject.Controllers
{
    //게시글 컨트롤러
    public class PostsController : Controller
    {
		public string UploadDir { get; set; }

		private readonly IWriteRepository writeRepository;
        private readonly MovieDbContext movieDbContext;

        public PostsController(IWriteRepository writeRepository, IWebHostEnvironment env)
        {
            this.writeRepository = writeRepository;
			UploadDir = Path.Combine(env.ContentRootPath, "MyFiles");
		}

        // GET: Posts/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Posts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePostViewModel createPostViewModel)
        {
            string userId = HttpContext.Session.GetString("UserId");

            // Validation
            createPostViewModel.ErrorCheck();
            if (createPostViewModel.HasError)
            {
                TempData["TitleError"] = createPostViewModel.ErrorTitle;
                TempData["ContentError"] = createPostViewModel.ErrorContent;

                // 사용자가 입력했던 데이터
                TempData["Title"] = createPostViewModel.Title;
                TempData["Content"] = createPostViewModel.Content;

                return View(createPostViewModel);
            }

            var post = new Post
            {
                Title = createPostViewModel.Title,
                Content = createPostViewModel.Content,
                Date = DateTime.Now,
                ViewCnt = createPostViewModel.ViewCnt,
                LikeCnt = createPostViewModel.LikeCnt,
                UserId = long.Parse(userId),
                BoardId = 1,
            };

            post = await writeRepository.AddPostAsync(post); // 게시물 추가

            if (createPostViewModel.Attachments != null && createPostViewModel.Attachments.Count() > 0)
            {
                var attachments = createPostViewModel.Attachments.ToList();

                await Upload(createPostViewModel.FileTitle, attachments, post.Id); // 파일 업로드
            }

            TempData["PostId"] = post.Id.ToString();

            return RedirectToAction("PostDetail", new { id = post.Id });
        }

		[HttpGet]
		public async Task<IActionResult> Update(long id)
		{
			
			var post = await writeRepository.GetPostAsync(id);
			if (post == null) return View(null);

			CreatePostViewModel postrequest = new()
			{
				Title = (string)TempData["Title"] ?? post.Title,
				Content = (string)TempData["Content"] ?? post.Content,				
			};

			return View(postrequest);
		}

        public async Task<IActionResult> Update(CreatePostViewModel createPostViewModel)
        {
            string userId = HttpContext.Session.GetString("UserId");

            // Validation
            createPostViewModel.ErrorCheck();
            if (createPostViewModel.HasError)
            {
                TempData["TitleError"] = createPostViewModel.ErrorTitle;
                TempData["ContentError"] = createPostViewModel.ErrorContent;

                // 사용자가 입력했던 데이터
                TempData["Title"] = createPostViewModel.Title;
                TempData["Content"] = createPostViewModel.Content;

                return RedirectToAction("Update");
            }

            var existingPost = await writeRepository.GetPostAsync(createPostViewModel.Id);
            if (existingPost == null)
            {
                // 수정 실패하면 List 로
                return RedirectToAction("Update");
            }

            existingPost.Title = createPostViewModel.Title;
            existingPost.Content = createPostViewModel.Content;
            existingPost.Date = DateTime.Now;

            var updateWrite = await writeRepository.UpdatePostAsync(existingPost);

      

            if (createPostViewModel.Attachments != null && createPostViewModel.Attachments.Count() > 0)
            {
                var attachments = createPostViewModel.Attachments.ToList();

                await Upload(createPostViewModel.FileTitle, attachments, existingPost.Id); // 파일 업로드
            }

            if (updateWrite == null)
            {
                // 수정 실패하면 List 로
                return RedirectToAction("Update");
            }

            TempData["UpdateSuccess"] = true;
            return RedirectToAction("PostDetail", new { id = createPostViewModel.Id });
        }

        [HttpPost]
        public async Task<IActionResult> Upload(string fileTitle, IList<IFormFile> attachments, long postId)
        {
            // 업로드 디렉토리 존재 확인
            DirectoryInfo di = new(UploadDir);
            // 없는 경우 디렉토리 생성
            if (di.Exists == false) di.Create();

            foreach (var formFile in attachments)
            {
                if (formFile.Length > 0)
                {
                    string savedFileName = formFile.FileName; // 저장할 파일명
                    var fileFullPath = Path.Combine(UploadDir, savedFileName);

                   
                    int filecnt = 1;
                    while (new FileInfo(fileFullPath).Exists)
                    {
                        var idx = formFile.FileName.LastIndexOf(".");
                        if (idx > -1)
                        {
                            var left = formFile.FileName.Substring(0, idx);
                            savedFileName = left + string.Format("({0})", filecnt++) + formFile.FileName.Substring(idx);
                        }
                        else
                        {
                            savedFileName = formFile.FileName + string.Format("({0})", filecnt++);
                        }

                        fileFullPath = Path.Combine(UploadDir, savedFileName);
                    }

                    // 파일업로드
                    using FileStream stream = new(fileFullPath, FileMode.Create);
                    await formFile.CopyToAsync(stream);

                    // DB 저장
                    var attachment = new Attachment
                    {
                        Title = fileTitle,
                        FileName = savedFileName,   // 저장된 파일명
                        OriginalName = formFile.FileName,  // 원본 파일명
                        PostId = postId, // 게시물의 postId 할당
                    };

                    await writeRepository.AddAttachmentAsync(attachment);



                }

            } // end foreach

			return RedirectToAction("Board/list/1");
		} // end action

        public async Task<IActionResult> Download(long Id)
        {
            Attachment? file = await writeRepository.GetAttachmentId(Id);

            if (file == null)
            {
                return RedirectToAction("Board/list/1");
            }

            // 물리적인 파일경로
            string fileFullPath = Path.Combine(UploadDir, file.FileName);

            try
            {
                // 파일을 바이트[] 로 읽음 (파일 읽기.  실패하면 예외 발생)
                byte[] bytes = await System.IO.File.ReadAllBytesAsync(fileFullPath);

                // 파일다운로드
                return File(
                        bytes,
                        MimeUtility.GetMimeMapping(fileFullPath),
                        file.OriginalName   // 실제 다운로드될 이름! 원본이름으로 다운로드
                    );
            }
            catch (Exception ex)
            {
                var msg = $"파일 다운로드 예외 발생: {fileFullPath}";
                await Console.Out.WriteLineAsync(msg);
                return NotFound(msg);  // 보통은 redirect 혹은 error view 를 리턴.
            }
        }


        [HttpGet("posts/postdetail/{Id}")]
        public async Task<IActionResult> PostDetail(long id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var post = await writeRepository.IncViewCntAsync(id); // 조회수 증가
            if (post == null)
            {
                return RedirectToAction("List");
            }

            var attachments = await writeRepository.GetAttachmentByPostIdAsync(id);

			post.Attachments = attachments;

			attachments.ForEach(x =>
			{
				x.RequestPath = $"/appfiles/{x.FileName}";

				// 물리적인 경로
				string fileFullPath = Path.Combine(UploadDir, x.FileName);

				try
				{
					// 파일 용량
					x.Size = new FileInfo(fileFullPath).Length;

					// 파일 타입 확인
					// MimeMapping 패키지 사용
					x.ContentType = MimeUtility.GetMimeMapping(fileFullPath);

					// 이미지 여부 확인
					// Image.Identify(파일경로) -> 이미지가 아닌 경우 UnknownImageFormatException 발생
					ImageInfo imageInfo = Image.Identify(fileFullPath);
					x.IsImage = true;
					x.Description = $"이미지] {imageInfo.Width} x {imageInfo.Height}";
				}
				catch (UnknownImageFormatException ex)
				{
					x.Description = "이미지가 아닙니다";
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
					var msg = $"파일 읽기 예외 발생 {fileFullPath}";
					Console.WriteLine(msg);
					x.Description = msg;
				}

			});

			if (post != null)
            {
                var comments = await writeRepository.GetIdCommentAsync(id);
                post.Comments = comments?.ToList();
               
            }

            ViewData["page"] = HttpContext.Session.GetInt32("page") ?? 1;

            return View(post);
        }

        // posts/userspostlist/userid
        [HttpGet("posts/userspostlist/{userId}")]
        public async Task<IActionResult> UsersPostList(long userId)
        {
             userId = long.Parse(HttpContext.Session.GetString("UserId"));

            var posts = await writeRepository.GetUserPostAsync(userId);
            return View(posts);
        }

		[HttpPost]
		public async Task<IActionResult> Delete(long commentid)
		{
			var deleteUser = await writeRepository.DeletePostAsync(commentid);

			if (deleteUser != null)
			{
				// 삭제 성공
				return View("Delete", 1);
			}

			// 삭제 실패
			return View("Delete", 0);

		}



        [HttpPost]
        public async Task<IActionResult> DeleteSelectedPosts(string selectedPostIds)
        {
            if (!string.IsNullOrEmpty(selectedPostIds))
            {
                var postIds = selectedPostIds.Split(',')
                    .Select(id => long.TryParse(id, out long postId) ? postId : 0)
                    .Where(postId => postId != 0)
                    .ToList();

                await writeRepository.DeleteSelectedPosts(postIds);
                return View("DeletePost", 1);
            }

            return View("DeletePost", 0);
        }

    }


}
