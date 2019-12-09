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
	_s7:	db	"The string you entered is...",0x0d,0x0a,0
	_s0:	db	"Beginning Program",0x0d,0x0a,0
	_s6:	db	"The meaning of life, the univers and everything is...",0x0d,0x0a,0
	_s8:	db	" ",0x0d,0x0a,0
	_s2:	db	"The value entered was ",0x0d,0x0a,0
	_s5:	db	" ",0x0d,0x0a,0
	_s1:	db	"Please enter an integer.",0x0d,0x0a,0
	_s3:	db	" ",0x0d,0x0a,0
	_s4:	db	"Please enter a string",0x0d,0x0a,0
;-----------------------------
; uninitialized data
;-----------------------------
section .bss USE32
	val1:	resd	1
	astring:	resb	128
	val2:	resd	1
;-----------------------------
; code
;-----------------------------
section .code USE32
_main:
	push	_s0
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	_s1
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	val1
	push	formatIntIn
	call	_scanf
	add	esp,	0x08
	push	_s2
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	DWORD[val1]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	push	_s3
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	_s4
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	astring
	push	formatStrIn
	call	_scanf
	add	esp,	0x08
	push	_s5
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	mov	DWORD[val2],	42
	push	_s6
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	DWORD[val2]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	push	_s7
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	astring
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	_s8
	push	stringPrinter
	call	_printf
	add	esp,	0x08
exit:
	mov	eax,	0x0
	call	_ExitProcess@4
