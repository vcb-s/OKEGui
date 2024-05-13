using System;
using System.Collections.Generic;

namespace OKEGui.Utils
{
    public class SliceInfo
    {
        public int begin;
        public int end;

        public bool CheckIllegal()
        {
            if (begin >= 0 && end >= 0 && begin < end)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public int CompareTo(SliceInfo s)
        {
            if (this.begin >= s.begin)
                return 1;
            else
                return -1;
        }
    }

    public class SliceInfoArray : List<SliceInfo>
    {
        public void Sorted()
        {
            this.Sort((x, y) => x.CompareTo(y));
        }

        // 用于从json直接读取的切片，检查各个切片之间是否有覆盖，将连续的区间进行合并
        // 注意确保在调用前已经按begin升序排序，并检查过各切片内部合法
        public SliceInfoArray CheckAndMerge()
        {
            SliceInfoArray res = new SliceInfoArray();
            SliceInfo prev_s = new SliceInfo();
            for (int i = 0; i < this.Count; i++)
            {
                if (i == 0)
                {
                    prev_s = this[i];
                }
                else
                {
                    if (this[i].begin < prev_s.end)
                        return null;
                    else if (this[i].begin == prev_s.end)
                        prev_s.end = this[i].end;
                    else
                    {
                        res.Add(new SliceInfo{
                            begin = prev_s.begin,
                            end = prev_s.end
                        });
                        prev_s = this[i];
                    }
                }
            }
            res.Add(new SliceInfo{
                begin = prev_s.begin,
                end = prev_s.end
            });
            return res;
        }

        public override string ToString()
        {
            string str = "[ ";
            foreach (var s in this)
            {
                str += $"[{s.begin}, {s.end}], ";
            }
            str += "]";
            return str;
        }
    }
}
