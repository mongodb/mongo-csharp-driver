+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Upgrading"
[menu.main]
  parent = "What's New"
  identifier = "Upgrading"
  weight = 10
  pre = "<i class='fa'></i>"
+++

## Breaking Changes

A bug in 2.9.0 prevents applications from connecting to replica sets via SRV. Applications connecting to replica sets over SRV should NOT upgrade to 2.9.0 and instead should upgrade directly to 2.9.1 or later.
