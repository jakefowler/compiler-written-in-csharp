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
	_s1:	db	"Enter a string: ",0x0d,0x0a,0
	_s0:	db	"assignment text:",0x0d,0x0a,0
;-----------------------------
; uninitialized data
;-----------------------------
section .bss USE32
	x:	resd	1
	result:	resd	1
	z:	resd	1
	astring:	resb	128
	_i2:	resd	1
	_i9:	resd	1
	_i6:	resd	1
	_i7:	resd	1
	_i8:	resd	1
	_i0:	resd	1
	_i5:	resd	1
	_i1:	resd	1
	_i3:	resd	1
	_i4:	resd	1
	y:	resd	1
;-----------------------------
; code
;-----------------------------
section .code USE32
_main:
	mov	DWORD[x],	3
	mov	DWORD[y],	4
	mov	DWORD[z],	5
	mov	edi,	DWORD[x]
	imul	edi,	DWORD[y]
	mov	DWORD[_i0],	edi
	mov	edi,	DWORD[_i0]
	imul	edi,	DWORD[z]
	mov	DWORD[_i1],	edi
	mov	eax,	DWORD[_i1]
	mov	DWORD[result],	eax
	push	_s0
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	DWORD[result]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	mov	esi,	-121
	mov	DWORD[result],	esi
	push	DWORD[result]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	mov	edi,	77
	imul	edi,	DWORD[y]
	mov	DWORD[_i5],	edi
	mov	esi,	33
	sub	esi,	DWORD[_i5]
	mov	DWORD[result],	esi
	push	DWORD[result]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	mov	edi,	154
	imul	edi,	DWORD[y]
	mov	DWORD[_i8],	edi
	mov	edi,	DWORD[_i8]
	imul	edi,	-1
	mov	DWORD[_i9],	edi
	mov	esi,	DWORD[y]
	add	esi,	33
	sub	esi,	DWORD[_i9]
	mov	DWORD[result],	esi
	push	DWORD[result]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	push	_s1
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	astring
	push	formatStrIn
	call	_scanf
	add	esp,	0x08
	push	astring
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	mov	esi,	DWORD[astring]
	add	esi,	" bob"
	mov	DWORD[astring],	esi
	push	astring
	push	stringPrinter
	call	_printf
	add	esp,	0x08
exit:
	mov	eax,	0x0
	call	_ExitProcess@4
