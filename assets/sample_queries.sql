-- Basic preview
select *
from orders
limit 10;

-- Revenue by region
select
  region,
  sum(amount) as total_amount,
  count(*) as order_count
from orders
where status = 'paid'
group by region
order by total_amount desc;

-- Join orders with customers
select
  c.customer_name,
  c.segment,
  o.region,
  sum(o.amount) as total_amount
from orders o
join customers c on o.customer_id = c.customer_id
where o.status = 'paid'
group by c.customer_name, c.segment, o.region
order by total_amount desc;

-- Join orders with products
select
  p.category,
  p.product_name,
  sum(o.quantity) as total_quantity,
  sum(o.amount) as total_amount
from orders o
join products p on o.product_id = p.product_id
where o.status = 'paid'
group by p.category, p.product_name
order by total_amount desc;

-- Time bucket by 1 minute
select
  date_bin(
    interval '1 minute',
    cast(order_time as timestamp),
    timestamp '1970-01-01 00:00:00'
  ) as bucket,
  sum(amount) as total_amount,
  count(*) as event_count
from orders_timeseries
where status = 'paid'
group by bucket
order by bucket;

-- Time bucket by 1 hour
select
  date_bin(
    interval '1 hour',
    cast(order_time as timestamp),
    timestamp '1970-01-01 00:00:00'
  ) as bucket,
  sum(amount) as total_amount,
  count(*) as event_count
from orders_timeseries
where status = 'paid'
group by bucket
order by bucket;

-- Time bucket by region and 1 hour
select
  date_bin(
    interval '1 hour',
    cast(order_time as timestamp),
    timestamp '1970-01-01 00:00:00'
  ) as bucket,
  region,
  sum(amount) as total_amount
from orders_timeseries
where status = 'paid'
group by bucket, region
order by bucket, region;
