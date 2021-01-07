using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace WLtaka_robot_async_app
{

    struct Axis
    {
        public float J1, J2, J3, J4, J5, J6;
        public Axis(float J1, float J2, float J3, float J4, float J5, float J6)
        {
            this.J1 = J1;
            this.J2 = J2;
            this.J3 = J3;
            this.J4 = J4;
            this.J5 = J5;
            this.J6 = J6;

        }



    }
    struct Coor
    {
        public float X, Y, Z, A, B, C;

        public Coor(float X, float Y, float Z, float A, float B, float C)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            this.A = A;
            this.B = B;
            this.C = C;

        }


    }

    enum MoveType
    {
        ABS = 90
   , INC = 91
    }


    class WLtaka
    {
        private string _comport;
        private int _baurate;

        private readonly SerialPort _serialport;


        private static float _tol = 0.1F;



        // public delegate void MovingStateHandler(string);
        //
        // public event MovingStateHandler MovingEvent;


        public float Tolerance
        {
            get { return _tol; }
            set { _tol = value; }

        }


        public WLtaka(string comport = "COM1", int baurate = 115200, float Tolerance = 0.05F)
        {
            _comport = comport;
            _baurate = baurate;
            _tol = Tolerance;

            _serialport = new SerialPort(_comport, _baurate);
            _serialport.Open();
            _serialport.WriteTimeout = 200;
            _serialport.ReadTimeout =500;
           
        }


        private static string Encode(byte[] dy, int count)
        {
            return Encoding.UTF8.GetString(dy, 0, count);
        }

        private static byte[] Decode(string cmd)
        {
            return Encoding.UTF8.GetBytes(cmd);
        }

        private void Write(string cmd)
        {
          
            _serialport.Write(cmd);

        }


        private string Read()
        {
            byte[] res = new byte[200];
            int len =_serialport.Read(res, 0, res.Length);
            return Encode(res, len);

        }


      


        private string OneProcess(string cmd)
        {
            try
            {
                 Write(cmd);
               
                   // Console.WriteLine("write done");
                
            }
            catch (TimeoutException tex)
            {
                return "write error:" + tex.Message;
            }
            StringBuilder msg = new StringBuilder();

            while ( true)

            {
                try
                {
                    // Console.WriteLine("begin read");
                    var str = Read();
                    msg.Append(str);
                }
                catch (TimeoutException tex)
                {
                    break;
                }
            }
            return msg.ToString();
        }

        public string AskStatus()
        {
           
         return OneProcess("?");
           
        }



        public string Homeing()
        {
            string cmd = "$H";
           return OneProcess(cmd);
           
         

        }

       
        public string MoveJonts(MoveType moveType, Axis axis, float Feedrate)
        {
            var tp = moveType == MoveType.ABS ? "G90" : "G91";


            string cmd = $"M21 {tp} G1 X{axis.J1} Y{axis.J2} Z{axis.J3} A{axis.J4} B{axis.J5} C{axis.J6} F{Feedrate}";
            return  OneProcess(cmd);
           
        }
        public string MoveCoors(MoveType moveType, Coor carti, float Feedrate)
        {
            var tp = moveType == MoveType.ABS ? "G90" : "G91";

            string cmd = $"M20 {tp} G1 X{carti.X} Y{carti.Y} Z{carti.Z}" +
                $" A{carti.A} B{carti.B} C{carti.C} F{Feedrate}";
            return OneProcess(cmd);
           

        }

        public Tuple<Axis, Coor> GetInfo()
        {
            var str = AskStatus();
            Axis a=new Axis();
            Coor b=new Coor();
           var strry= str.Split(',',':');
            if (strry.Length > 12)
            {
                 a = GetAxis(strry[6], strry[7], strry[8], strry[2], strry[3], strry[4]);
                 b = GetCoor(strry[10], strry[11], strry[12], strry[13], strry[14], strry[15]);
            }

            return Tuple.Create<Axis, Coor>(a, b);
        }

        private Axis GetAxis(string j1, string j2, string j3, string j4, string j5, string j6)
        {
            var J1 = Convert.ToSingle(j1);
            var J2 = Convert.ToSingle(j2);
            var J3 = Convert.ToSingle(j3);
            var J4 = Convert.ToSingle(j4);
            var J5 = Convert.ToSingle(j5);
            var J6 = Convert.ToSingle(j6);

            return new Axis(J1, J2, J3, J4, J5, J6);
        
        }

        private Coor GetCoor(string x, string y, string z, string a, string b, string c)
        {
            var X = Convert.ToSingle(x);
            var Y = Convert.ToSingle(y);
            var Z = Convert.ToSingle(z);
            var A = Convert.ToSingle(a);
            var B = Convert.ToSingle(b);
            var C = Convert.ToSingle(c);
            return new Coor(X, Y, Z, A, B, C);
        
        
        }


        
        private static bool CheckingJoints(Axis nominal, Axis current)
        {
            float TOL = 0.001F;

            return Math.Abs(nominal.J1 - current.J1) < TOL &
                 Math.Abs(nominal.J2 - current.J2) < TOL &
                 Math.Abs(nominal.J3 - current.J3) < TOL &
                 Math.Abs(nominal.J4 - current.J4) < TOL &
                 Math.Abs(nominal.J5 - current.J5) < TOL &
                 Math.Abs(nominal.J6 - current.J6) < TOL;



        }


        /*
        public async Task MultiPointsRun(Cartinan[] points, CancellationToken cancel)
        {
            foreach (var pt in points)
            {

                var res = await MoveCoors(MoveType.ABS, pt, 2000);

                cancel.ThrowIfCancellationRequested();
            }

        }

        */

        private static bool CheckingInPostion(Coor nominal, Coor current)
        {
            var dev = Math.Sqrt(
                (nominal.X - current.X) * (nominal.X - current.X) +
                 (nominal.Y - current.Y) * (nominal.Y - current.Y) +
                  (nominal.Z - current.Z) * (nominal.Z - current.Z)
                  );

            return dev <= _tol ? true : false;
        }







    }
}

