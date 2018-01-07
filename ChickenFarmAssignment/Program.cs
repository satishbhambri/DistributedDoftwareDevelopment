using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using System.Text;

namespace ChickenFarmAssignment
{
    public class Program
    {
        public delegate void priceCutEvent(Int32 pr);
        public delegate void confirmationOrderEvent(Int32 TotalAmount, int id);

        static Semaphore sembuffersync = new Semaphore(0, 3);
        public static Int32 Amount;
        static int count = 0;

        class ChickenFarm
        {
            static Random rng = new Random();
            public static event priceCutEvent priceCut;
            private static Int32 chickenPrice = 10;

            public Int32 getPrice()
            {
                return chickenPrice;
            }

            //pricecutevent emits an event and calls event handlers in Retailers
            //Implement a method for recieving order from multicellbuffer

            public void GetOrderfromBuffer(String val)
            {
                OrderClass order = new OrderClass();
                order = Retailer.Decoder(val);
                OrderProcessing obj = new OrderProcessing();
                obj.calculate(order);
            }

            //a function call to decoder to convert the string recieved from multicellbuffer to object.
            //for each order we have a new thread from order processing class to process the order based on the current price. 
            //Counter variable count, when count = 10 chicken farm terminates. 

            public void farmerFunc()
            {
                while (count < 10)
                {
                    int price = PricingModel();
                    Thread.Sleep(500);
                    ChickenFarm.changePrice(price);
                }
            }

            public static int PricingModel()
            {
                //decides the price of the chicken
                Int32 p = rng.Next(5, 15);
                return p;
            }


            class OrderProcessing
            {
                public static event confirmationOrderEvent orderEvent;

                public void calculate(OrderClass obj)
                {
                    Thread orderthread = new Thread(() => ordercal(obj));
                    orderthread.Start();
                    //Console.WriteLine("Order Thread computed");
                    Retailer r = new Retailer();
                    orderEvent += new confirmationOrderEvent(r.amountHandler);
                }

                static int retailer_id = 0;

                public static void ordercal(OrderClass obj)
                {
                    int totalAmount = 0;
                    int unitprice = chickenPrice;
                    int noofchickens = obj.getAmount();
                    int tax = 10;
                    int shipping = 20;
                    int cno = obj.getCardNo();
                    if (cno > 5000 && cno < 7000)
                    {
                        totalAmount = unitprice * noofchickens + tax + shipping;
                    }

                    orderprocessed(totalAmount);
                    retailer_id = obj.getSenderId();
                    //Console.WriteLine("And the total amount is : " + totalAmount);
                    //Console.WriteLine("Unit Price : " + unitprice);
                    //Console.WriteLine("No. of chicks : " + noofchickens);
                }


                public static void orderprocessed(Int32 TotalAmount)
                {
                    if (orderEvent != null)
                    {
                        // Console.WriteLine("Total Amount : {0}, Id : {1}", TotalAmount, retailer_id)
                        orderEvent(TotalAmount, retailer_id);
                    }
                    Amount = TotalAmount;
                }

                //Instantiate the thread for each order object
                //check the validity of credit card number
                //each thread calculates total charges = unit price * no of chicks + tax + shipping ;
                //send confirmation(using callback) to retailer
            }


            public static void changePrice(Int32 price)
            {
                if (price < chickenPrice)
                {
                    if (priceCut != null)
                    {
                        priceCut(price);
                        count++;
                    }
                }
                chickenPrice = price;
            }



            class Retailer
            {
                //5 retailers in the main class
                // actions are event driven
                // contains a callback method chicken on sale for chicken farm to call when price cut event happens
                //The retailer will calculate the number of chickens 
                //create an orderclass object for each order based on..
                // Call encoder with the order object as a parameter
                // The encoder string is send back to the retailer
                // call multicell buffer with string as the paramter
                // have a timestamp before sending the orders
                // get the confirmation from order processing class and save the time. 

                public static string Encoder(OrderClass obj)
                {
                    //Console.WriteLine("Entered Encoder");
                    String SenderID = Convert.ToString(obj.getSenderId());
                    String CardNo = Convert.ToString(obj.getCardNo());
                    String Amount = Convert.ToString(obj.getAmount());
                    StringBuilder str = new StringBuilder();
                    str.Append(SenderID + "," + CardNo + "," + Amount);
                    //Console.WriteLine("The string is :");
                    // Console.WriteLine(str.ToString());
                    return str.ToString();
                }

                public static OrderClass Decoder(String str)
                {
                    OrderClass ob = new OrderClass();
                    char[] sep = { ',' };
                    String[] values = str.Split(sep);
                    ob.setSenderId(Convert.ToInt32(values[0]));
                    ob.setCardNo(Convert.ToInt32(values[1]));
                    ob.setAmount(Convert.ToInt32(values[2]));

                    return ob;
                }
                public void retailerFunc()
                {
                    Thread.Sleep(500);
                    ChickenFarm chicken = new ChickenFarm();
                    OrderClass ob = new OrderClass();
                    int senderId = Convert.ToInt32(Thread.CurrentThread.Name);
                    int cardNumber = rng.Next(5000, 7000);
                    int amount = rng.Next(50, 100);
                    ob.setSenderId(senderId);
                    ob.setCardNo(cardNumber);
                    ob.setAmount(amount);

                    //Now we should send this object to the encoder to convert into string
                    String Encoder_result = Encoder(ob);
                    MultiCellBuffer cell_order = new MultiCellBuffer();
                    cell_order.setOneCell(Encoder_result);
                }

                public void amountHandler(Int32 price, Int32 id)
                {
                    string iden = Thread.CurrentThread.Name;
                    DateTime time1 = DateTime.Now;
                    Console.WriteLine("Order of the Retailer has been processed with following details:\n");
                    // Console.WriteLine("\n");
                    Console.WriteLine("Confirmation recieved for \n Amount ${0} at the time {1}.", price, time1);
                    Thread.Sleep(500);
                    Console.WriteLine("Order Completed");
                }

                public void chickenOnSale(Int32 p)
                {
                    string stno = Thread.CurrentThread.Name;
                    Console.WriteLine("\nStores are on sale as low as ${0} each", p);
                    //Event handler for callback of pricecutevent event
                    //
                }
            }
            class OrderClass
            {
                private int senderId, cardNo, Amount;

                public void setSenderId(int senderId)
                {
                    this.senderId = senderId;
                }

                public int getSenderId()
                {
                    return senderId;
                }
                public void setCardNo(int cardNo)
                {
                    this.cardNo = cardNo;
                }

                public int getCardNo()
                {
                    return cardNo;
                }

                public void setAmount(int Amount)
                {
                    this.Amount = Amount;
                }

                public int getAmount()
                {
                    return Amount;
                }

            }
            class QueueCell : Queue<string>// defines a fixed size queue
            {
                public int size { get; set; }
                public QueueCell(int s)
                {
                    size = s;
                }

                public void EnqueueOne(string s)
                {
                    if (this.Count < this.size)
                    {
                        this.Enqueue(s);
                    }
                    else
                    {
                        Console.WriteLine("Queue is full for this thread, therefore, dequeueing first and then trying Enqueue:");
                        this.Dequeue();
                        sembuffersync.Release();
                        EnqueueOne(s);
                    }
                }
            }
            class MultiCellBuffer
            {
                //n data cells, typically n = 3
                //number of cells is less than the max no N = 5, no of retailers

                public static QueueCell queue = new QueueCell(3);

                public void setOneCell(String s)
                {
                    try
                    {

                        sembuffersync.WaitOne(3);//If semaphore is zero then wait until it becomes non zero
                        lock (queue)
                        {
                            //Console.WriteLine("Entered lock : ");
                            queue.EnqueueOne(s);

                            // Console.WriteLine("Enqued successfully : ");

                            //Implement an enqueue
                            //Once one cell is set break out of the loop and not to set all other empty cells.     
                        }
                        string valtobepassed = getOneCell();
                        ChickenFarm order = new ChickenFarm();
                        order.GetOrderfromBuffer(valtobepassed);

                        //need to update the CellArray's empty position with the incoming string from the ChickenFarm/Retailer
                        // Call setOneCell and getOneCell methods 
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine(ex.Message);
                    }
                }


                public static string getOneCell()
                {
                    //Console.WriteLine("Entered Dequeue");
                    string val;
                    lock (queue)
                    {
                        val = queue.Dequeue();
                        //dequeue functionality
                        sembuffersync.Release();
                    }
                    return val;

                    // Increment semaphore once the resource is released
                    //implement a queue function, first in first out
                    // used to read data from one of the available cells
                }

                // define the global semaphore of value n to manage the cells and use lock for each cell to ensure the synchronization
            }

            static void Main(string[] args)
            {
                try
                {

                    //create buffer classes, instantiate objects, create and start threads 
                    ChickenFarm chicken = new ChickenFarm();
                    Thread farmer = new Thread(new ThreadStart(chicken.farmerFunc));
                    farmer.Start();
                    Retailer chickenstore = new Retailer();
                    ChickenFarm.priceCut += new priceCutEvent(chickenstore.chickenOnSale);
                    Thread[] retailers = new Thread[5];
                    for (int i = 0; i < 5; i++)
                    {
                        retailers[i] = new Thread(new ThreadStart(chickenstore.retailerFunc));
                        retailers[i].Name = (i + 1).ToString();
                        retailers[i].Start();
                    }
                }
                catch (Exception ex)
                {
                    // Console.WriteLine(ex.Message);
                }

            }
        }
    }
}
