## MongoDB C# Driver LINQ Version

driver home: http://github.com/mongodb/mongo-csharp-driver

mongodb home: http://www.mongodb.org/

apidoc: http://api.mongodb.org/csharp/ (coming soon)


*Note this is not the official mongo c# driver. This is a branch of the current release of the mongo driver which supports LINQ 

This is a branch of the original driver however this additionally supports operations on collections such as:


            var items =  from item in db.GetCollection(settings)
                        where item.ID == 123 || item.Name == "Hi"
                        select item;

Also certain operations such as:

* db.GetCollection(settings).Count()
* db.GetCollection(settings).LongCount()
* db.GetCollection(settings).Take(10).Skip(10);
* db.GetCollection(settings).OrderBy(k=>k.ID)
* db.GetCollection(settings).OrderByDescending(k=>k.Order);
* db.GetCollection(settings).Select(k => new {Name=k.Name + k.ID})
* db.GetCollection(settings).Where(k => k.ID == 1 && k.Price > 10.00)
* db.GetCollection(settings).FirstOrDefault();
* db.GetCollection(settings).LastOrDefault();
* db.GetCollection(settings).Reverse();





### Maintainers:
* Vlad Shlosberg            vshlos@gmail.com


### Original Contributors to mongo csharp driver:
* Bit Diffusion Limited     code@bitdiff.com
* Justin Dearing            zippy1981@gmail.com
* Teun Duynstee             teun@duynstee.com
* Ken Egozi                 mail@kenegozi.com
* Simon Green               simon@captaincodeman.com
* Brian Knight              brianknight10@gmail.com  
* Richard Kreuter           richard@10gen.com
* Kevin Lewis               kevin.l.lewis@gmail.com
* Dow Liu                   redforks@gmail.com
* Andrew Rondeau            github@andrewrondeau.com
* Ed Rooth                  edward.rooth@wallstreetjapan.com
* Testo                     test1@doramail.com   
* Craig Wilson              craiggwilson@gmail.com
