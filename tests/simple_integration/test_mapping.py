from pathlib import Path
import sqlite3
import unittest

sql = (Path(__file__).resolve().parents[2] / 'database/simple_integration/02_get_permit_info.sql').read_text()
query = sql[sql.index(' WITH QUEUE_PROCESS_MAP'):sql.index(';\n -- Caller')]
query = query.replace('@LDMSDEV_LINK','').replace(' FROM dual','')
for name in ['p_type','p_id','p_queue','p_reference']:
    query = query.replace(name, ':' + name)

class Mapping(unittest.TestCase):
    def setUp(self):
        self.db = sqlite3.connect(':memory:')
        self.db.executescript('''CREATE TABLE folder(folderrsn,foldertype,referencefile);
CREATE TABLE folderprocess(folderrsn,processrsn,processcode,assigneduser,signoffuser,scheduledate,scheduleenddate,startdate,enddate,statuscode,passedflag);
INSERT INTO folder VALUES(1,'COM','B25901449');
INSERT INTO folderprocess(folderrsn,processrsn,processcode,assigneduser) VALUES(1,10,50099,'REVIEWER'),(1,11,999999,'OTHER');''')
    def rows(self, queue):
        return self.db.execute(query, dict(p_type='A',p_id=20,p_queue=queue,p_reference='B25901449')).fetchall()
    def test_mapped_process(self):
        r=self.rows(10001)
        self.assertEqual(len(r),1)
        self.assertEqual(r[0][6:9],(10,50099,'REVIEWER'))
    def test_disabled(self): self.assertEqual(self.rows(10420),[])
    def test_unmapped(self): self.assertEqual(self.rows(-1),[])
    def test_no_process_code(self): self.assertEqual(self.rows(10009),[])
    def test_single_process_fallback(self):
        self.db.execute('DELETE FROM folderprocess WHERE processrsn=11')
        self.assertEqual(len(self.rows(10009)),1)

if __name__ == '__main__': unittest.main()
