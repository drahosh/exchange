Bitcoin exchange project for learning c#

assignment from https://docs.google.com/document/d/1V2myhqjxi_seDJlUChEq9saD0BtnAVX3dGQuE6csbxQ/edit

made in visual studio, using framework asp.net core, tests using MSTEST

endpoints are on localhost:64611, for example : "http://localhost:64611/api/Users/Register?name=test"

starting the project should also open a website with an error - ignore it
  (deleted the default weather projects site and didn't replace it since it wasn't needed for the assignment, if you close the window the project stops)
         
uses a psql database - couldn't get migrations to work with psql, so i created them manually.

create table orders(                                       
  id serial primary key,
  user_id int4,
  remaining_bitcoin_amount int4,
  total_bitcoin_amount int4,
  dollar_rate int4,
  status text,
  is_buy bool);
  
create table Users(
  id serial PRIMARY KEY,
  name TEXT,
  token TEXT,
  dollars int4,
  bitcoins int4);
