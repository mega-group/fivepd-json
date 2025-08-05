using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fivepd_json.models
{
    public class QuestionsConfig
    {
        public List<PedQuestionConfig> questions { get; set; }
    }

    public class PedQuestionConfig
    {
        public string question { get; set; }
        public List<string> answers { get; set; }
    }
}

