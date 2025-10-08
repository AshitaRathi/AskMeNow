using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AskMeNow.Application.Handlers
{
    public interface IQuestionHandler
    {
        Task<FAQAnswer> ProcessQuestionAsync(string question);
        Task<FAQAnswer> ProcessQuestionAsync(string question, string conversationId);
        Task<List<FAQDocument>> InitializeDocumentsAsync(string folderPath);
        FileProcessingResult? GetLastProcessingResult();
    }
}
