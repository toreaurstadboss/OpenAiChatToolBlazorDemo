namespace OpenAiChatToolBlazorDemo.Models
{
    
    public class TravelData
    {
        public int Id { get; set; }
        public List<string> CustomerNames { get; set; }
        public string Destination { get; set; }
        public string Hotel { get; set; }
        public string Flight { get; set; }
        public CostDetails Cost { get; set; }
        public DateTime TravelDate { get; set; }
        public DateTime ReturnDate { get; set; }
    }

}
