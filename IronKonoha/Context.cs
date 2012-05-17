using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronKonoha
{
    public enum ReportLevel
    {
        CRIT,
        ERR,
        WARN,
        INFO,
        PRINT,
        DEBUG,
    }

    public class Sugar{
        public List<object> Errors { get; set; }
        public int ErrorCount { get; set; }
        public Sugar()
        {
            Errors = new List<object>();
        }

        public int err_count { get; set; }
    }

    public class KMemShare
    {

    }

    public class KmemLocal
    {

    }

    public class KModShare
    {

    }

    public class KModLocal
    {
        public Sugar modsugar { get; set; }
    }

    public class KShare
    {

    }

    public class KArray : KObject
    {

    }

    public class KLocal
    {

    }

    public class KStack
    {

    }
    public class KLogger
    {

    }
    public class KObject
    {
        public object magicflag { get; set; }
        public KonohaClass kclass { get; private set; }
        public KArray kvproto { get; set; }
    }

    public class KNumber : KObject
    {
        protected object value;
        public KNumber(int val)
        {
            value = val;
        }
        public KNumber(uint val)
        {
            value = val;
        }
        public KNumber(long val)
        {
            value = val;
        }
        public KNumber(ulong val)
        {
            value = val;
        }
        public KNumber(double val)
        {
            value = val;
        }
        public KNumber(short val)
        {
            value = val;
        }
        public int ToInt()
        {
            return (int)(value ?? 0);
        }
        public uint ToUInt()
        {
            return (uint)(value ?? 0);
        }
        public long ToLong()
        {
            return (long)(value ?? 0);
        }
        public ulong ToULong()
        {
            return (ulong)(value ?? 0);
        }
        public float ToFloat()
        {
            return (float)(value ?? 0);
        }
        public double ToDouble()
        {
            return (double)(value ?? 0);
        }
        public bool ToBoolean()
        {
            return (bool)(value ?? false);
        }
        public override string ToString()
        {
            return value == null ? "" : value.ToString();
        }
    }

    public class KBoolean : KNumber
    {
        public KBoolean(bool val) : base(val ? 1 : 0)
        {
        }
    }
    
    public class Context
    {
        public KMemShare memshare { get; private set; }
        public KmemLocal memlocal { get; private set; }
        public KShare share { get; private set; }
        public KStack stack { get; private set; }
        public KLogger logger { get; private set; }
        public KModShare modshare { get; private set; }
        public KModLocal modlocal { get; private set; }
        public uint KErrorNo { get; private set; }
        public Sugar sugar { get { return modlocal.modsugar; } }

        public Context()
        {
        }

        public string GetErrorTypeString(ReportLevel pe)
        {
            switch (pe)
            {
                case ReportLevel.CRIT:
                case ReportLevel.ERR: return "(error)";
                case ReportLevel.WARN: return "(warning)";
                case ReportLevel.INFO:
                    throw new NotImplementedException();
                    /*if (CTX_isInteractive() || CTX_isCompileOnly() || verbose_sugar)
                    {
                        return "(info)";
                    }*/
                    return null;
                case ReportLevel.DEBUG:
                    throw new NotImplementedException();
                    /*if (verbose_sugar)
                    {
                        return "(debug)";
                    }*/
                    return null;
            }
            return "(unknown)";
        }

        public uint SUGAR_P(ReportLevel pe, LineInfo line, int lpos, string format, params object[] param)
        {
            
            return vperrorf(pe, line, lpos, format, param);
        }

        uint vperrorf(ReportLevel pe, LineInfo uline, int lpos, string fmt, params object[] ap)
        {
            string msg = GetErrorTypeString(pe);
            uint errref = unchecked((uint)-1);
            if (msg != null)
            {
                var sugar = this.sugar;
                if (uline != null)
                {
                    string file = uline.Filename;
                    Console.Write("%s (%s:%d) ", msg, file, uline.LineNumber);
                }
                else
                {
                    Console.Write("%s ", msg);
                }
                Console.Write(fmt, ap);
                errref = (uint)sugar.Errors.Count;
                sugar.Errors.Add(msg);
                if (pe == ReportLevel.ERR || pe == ReportLevel.CRIT)
                {
                    sugar.ErrorCount++;
                }
                ReportError(pe, msg);
            }
            return errref;
        }

        void ReportError(ReportLevel pe, string msg)
        {
            var color = Console.ForegroundColor;
            switch (pe)
            {
                case ReportLevel.CRIT:
                case ReportLevel.ERR:
                case ReportLevel.WARN:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case ReportLevel.INFO:
                case ReportLevel.PRINT:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                default:
                    break;
            }
            Console.WriteLine(" - " +  msg);
            Console.ForegroundColor = color;
        }

        public int sugarerr_count { get; set; }
    }
}
