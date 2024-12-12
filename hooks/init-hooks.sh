#!/bin/sh

dst=.git/hooks/post-checkout
ln -s -f ../../hooks/post-checkout ../"$dst"
cd ..
$dst
