;-----------------------------
; emports
;-----------------------------
global _main
EXPORT _main
;-----------------------------
; imports
;-----------------------------
extern _printf
extern _ExitProcess@4
;-----------------------------
; initialized data
;-----------------------------
section .data USE32
	stringPrinter	db	"%s",0
	numberPrinter	db	"%d",0x0d,0x0a,0
	_s0	db	"Hello Sarah",0x0d,0x0a,0
;-----------------------------
; uninitialized data
;-----------------------------
section .bss USE32
	num2:	resd	1
	aray:	resd	1
	num3:	resd	1
	str:	resb	128
	num1:	resd	1
;-----------------------------
; code
;-----------------------------
section .code USE32
_main:
	push	_s0
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	mov	DWORD[num1],	3
	mov	DWORD[num2],	-1
	push	DWORD[num2]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	push	1888
	push	numberPrinter
	call	_printf
	add	esp,	0x08
exit:
	mov	eax,	0x0
	call	_ExitProcess@4
