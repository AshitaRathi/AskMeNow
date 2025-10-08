using AskMeNow.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AskMeNow.Application.Services
{
    public interface IFAQService
    {
        Task<List<FAQDocument>> LoadDocumentsAsync(string folderPath);
        Task<FAQAnswer> AnswerQuestionAsync(string question);
        Task<FAQAnswer> AnswerQuestionAsync(string question, string conversationId);
    }
}
