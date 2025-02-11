# utility code for autotest

import os, pexpect, sys, time
from subprocess import call, check_call,Popen, PIPE


def topdir():
    '''return top of git tree where autotest is running from'''
    d = os.path.dirname(os.path.realpath(__file__))
    assert(os.path.basename(d)=='autotest')
    d = os.path.dirname(d)
    assert(os.path.basename(d)=='Tools')
    d = os.path.dirname(d)
    return d

def reltopdir(path):
    '''return a path relative to topdir()'''
    return os.path.normpath(os.path.join(topdir(), path))


def run_cmd(cmd, dir=".", show=False, output=False, checkfail=True):
    '''run a shell command'''
    if show:
        print("Running: '%s' in '%s'" % (cmd, dir))
    if output:
        return Popen([cmd], shell=True, stdout=PIPE, cwd=dir).communicate()[0]
    elif checkfail:
        return check_call(cmd, shell=True, cwd=dir)
    else:
        return call(cmd, shell=True, cwd=dir)

def rmfile(path):
    '''remove a file if it exists'''
    try:
        os.unlink(path)
    except Exception:
        pass

def deltree(path):
    '''delete a tree of files'''
    run_cmd('rm -rf %s' % path)



def build_SIL(atype):
    '''build desktop SIL'''
    run_cmd("make -f ../libraries/Desktop/Makefile.desktop clean all",
            dir=reltopdir(atype),
            checkfail=True)
    return True

def build_AVR(atype, board='mega2560'):
    '''build AVR binaries'''
    config = open(reltopdir('config.mk'), mode='w')
    config.write('''
BOARD=%s
PORT=/dev/null
''' % board)
    config.close()
    run_cmd("make clean", dir=reltopdir(atype),  checkfail=True)
    run_cmd("make", dir=reltopdir(atype),  checkfail=True)
    return True


# list of pexpect children to close on exit
close_list = []

def pexpect_autoclose(p):
    '''mark for autoclosing'''
    global close_list
    close_list.append(p)

def pexpect_close(p):
    '''close a pexpect child'''
    global close_list

    xvfb_server_num = getattr(p, 'xvfb_server_num', None)
    if xvfb_server_num is not None:
        kill_xvfb(xvfb_server_num)
    try:
        p.close()
    except Exception:
        pass
    time.sleep(1)
    try:
        p.close(force=True)
    except Exception:
        pass
    if p in close_list:
        close_list.remove(p)

def pexpect_close_all():
    '''close all pexpect children'''
    global close_list
    for p in close_list[:]:
        pexpect_close(p)

def start_SIL(atype, valgrind=False, wipe=False, CLI=False, height=None):
    '''launch a SIL instance'''
    cmd=""
    if valgrind and os.path.exists('/usr/bin/valgrind'):
        cmd += 'valgrind -q --log-file=%s-valgrind.log ' % atype
    cmd += reltopdir('tmp/%s.build/%s.elf' % (atype, atype))
    if wipe:
        cmd += ' -w'
    if CLI:
        cmd += ' -s'
    if height is not None:
        cmd += ' -H %u' % height
    ret = pexpect.spawn(cmd, logfile=sys.stdout, timeout=5)
    pexpect_autoclose(ret)
    ret.expect('Waiting for connection')
    return ret

def start_MAVProxy_SIL(atype, aircraft=None, setup=False, master='tcp:127.0.0.1:5760',
                       fgrate=200,
                       options=None, logfile=sys.stdout):
    '''launch mavproxy connected to a SIL instance'''
    global close_list
    MAVPROXY = reltopdir('../MAVProxy/mavproxy.py')
    cmd = MAVPROXY + ' --master=%s --fgrate=%u --out=127.0.0.1:14550' % (master, fgrate)
    if setup:
        cmd += ' --setup'
    if aircraft is None:
        aircraft = 'test.%s' % atype
    cmd += ' --aircraft=%s' % aircraft
    if options is not None:
        cmd += ' ' + options
    ret = pexpect.spawn(cmd, logfile=logfile, timeout=60)
    pexpect_autoclose(ret)
    return ret


def expect_setup_callback(e, callback):
    '''setup a callback that is called once a second while waiting for
       patterns'''
    def _expect_callback(pattern, timeout=e.timeout):
        tstart = time.time()
        while time.time() < tstart + timeout:
            try:
                ret = e.expect_saved(pattern, timeout=1)
                return ret
            except pexpect.TIMEOUT:
                e.expect_user_callback(e)
                pass
        print("Timed out looking for %s" % pattern)
        raise pexpect.TIMEOUT(timeout)

    e.expect_user_callback = callback
    e.expect_saved = e.expect
    e.expect = _expect_callback

def mkdir_p(dir):
    '''like mkdir -p'''
    if not dir:
        return
    if dir.endswith("/"):
        mkdir_p(dir[:-1])
        return
    if os.path.isdir(dir):
        return
    mkdir_p(os.path.dirname(dir))
    os.mkdir(dir)

def loadfile(fname):
    '''load a file as a string'''
    f = open(fname, mode='r')
    r = f.read()
    f.close()
    return r

def lock_file(fname):
    '''lock a file'''
    import fcntl
    f = open(fname, mode='w')
    try:
        fcntl.lockf(f, fcntl.LOCK_EX | fcntl.LOCK_NB)
    except Exception:
        return None
    return f

def kill_xvfb(server_num):
    '''Xvfb is tricky to kill!'''
    try:
        import signal
        pid = int(open('/tmp/.X%s-lock' % server_num).read().strip())
        print("Killing Xvfb process %u" % pid)
        os.kill(pid, signal.SIGINT)
    except Exception, msg:
        print("failed to kill Xvfb process - %s" % msg)
