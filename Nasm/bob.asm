;-----------------------------
; emports
;-----------------------------
global _main
EXPORT _main
;-----------------------------
; imports
;-----------------------------
extern _printf
extern _scanf
extern _ExitProcess@4
;-----------------------------
; initialized data
;-----------------------------
section .data USE32
	stringPrinter:	db	"%s",0
	numberPrinter:	db	"%d",0x0d,0x0a,0
	formatIntIn:	db	"%d",0
	formatStrIn:	db	"%s",0
	_s2:	db	"Testing reading in integers on the next line",0x0d,0x0a,0
	_s5:	db	"String read in is:",0x0d,0x0a,0
	_s1:	db	"Testing writing integers stored in variables on the next line",0x0d,0x0a,0
	_s4:	db	"Testing reading in strings on the next line",0x0d,0x0a,0
	_s3:	db	"Integer read in is:",0x0d,0x0a,0
	_s0:	db	"Testing writing integers on the next line",0x0d,0x0a,0
;-----------------------------
; uninitialized data
;-----------------------------
section .bss USE32
	num2:	resd	1
	num3:	resd	1
	num1:	resd	1
	str:	resb	128
;-----------------------------
; code
;-----------------------------
section .code USE32
_main:
	push	str
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	mov	DWORD[num1],	3
	mov	DWORD[num2],	-1
	push	_s0
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	1888
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	push	_s1
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	DWORD[num2]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	push	_s2
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	num1
	push	formatIntIn
	call	_scanf
	add	esp,	0x08
	push	_s3
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	DWORD[num1]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	push	_s4
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	str
	push	formatStrIn
	call	_scanf
	add	esp,	0x08
	push	_s5
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	str
	push	stringPrinter
	call	_printf
	add	esp,	0x08
exit:
	mov	eax,	0x0
	call	_ExitProcess@4
