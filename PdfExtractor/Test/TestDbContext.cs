using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfExtractor.Test
{
    public class TestDbContext
    {
        public string State { get; set; } = "1";
    }

    public class TestDomain
    {
        TestDbContext _db;
        public TestDomain(TestDbContext db)
        {
            _db = db;
        }

        public void Do()
        {
            Console.WriteLine(_db.State);
        }
    }
}
