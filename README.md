# ChilliCream.Snowflake

A lock-free, high-performance, thread-safe 64-bit unique ID generator for distributed systems — inspired by [Twitter's Snowflake algorithm](https://blog.twitter.com/engineering/en_us/a/2010/announcing-snowflake.html) and refined for .NET.

![NuGet](https://img.shields.io/nuget/v/ChilliCream.Snowflake)
![License](https://img.shields.io/github/license/ChilliCream/Snowflake)

---

## ✨ Features

- 🔒 Lock-free and thread-safe
- ⏱ 4096 IDs per millisecond, per machine
- 🏭 Supports up to 1024 nodes (32 datacenters × 32 machines)
- 📅 69+ years of sortable, timestamp-prefixed IDs
- ⚙️ Customizable epoch, datacenter, and machine identifiers
- 📦 Lightweight and dependency-free

---

## 📦 Install

```bash
dotnet add package ChilliCream.Snowflake