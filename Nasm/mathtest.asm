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
	_s1:	db	"val1 = 1 + 2 + 3 + 4",0x0d,0x0a,0
	_s6:	db	"this string was assigned to variable astring",0x0d,0x0a,0
	_s3:	db	"val1 = 20 / 5",0x0d,0x0a,0
	_s2:	db	"val2 = 3 * 4",0x0d,0x0a,0
	_s0:	db	"Beginning Program",0x0d,0x0a,0
	_s9:	db	"Please enter a number to multiply by 10",0x0d,0x0a,0
	_s4:	db	"val2 = 6 - 7 * (8+9)",0x0d,0x0a,0
	_s8:	db	"val2 := val1 * val1 / (7 - 6)",0x0d,0x0a,0
	_s5:	db	"val1 = (10 * (11 + 12) / 13 + (14 * 15))",0x0d,0x0a,0
	_s7:	db	"val1 = 5-3",0x0d,0x0a,0
;-----------------------------
; uninitialized data
;-----------------------------
section .bss USE32
	astring:	resb	128
	_i3:	resd	1
	_i11:	resd	1
	_i9:	resd	1
	_i5:	resd	1
	_i1:	resd	1
	val2:	resd	1
	_i12:	resd	1
	_i7:	resd	1
	_i8:	resd	1
	_i0:	resd	1
	_i6:	resd	1
	_i10:	resd	1
	val1:	resd	1
	_i2:	resd	1
	_i4:	resd	1
;-----------------------------
; code
;-----------------------------
section .code USE32
_main:
	push	_s0
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	mov	esi,	10
	mov	DWORD[val1],	esi
	push	_s1
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	DWORD[val1]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	mov	DWORD[val2],	12
	push	_s2
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	DWORD[val2]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	mov	DWORD[val1],	4
	push	_s3
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	DWORD[val1]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	mov	esi,	-113
	mov	DWORD[val2],	esi
	push	_s4
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	DWORD[val2]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	mov	DWORD[val1],	227
	push	_s5
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	DWORD[val1]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	push	_s6
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	astring
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	mov	esi,	2
	mov	DWORD[val1],	esi
	push	_s7
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	DWORD[val1]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	mov	edi,	DWORD[val1]
	imul	edi,	DWORD[val1]
	mov	DWORD[_i9],	edi
	xor	edx,	edx
	mov	eax,	DWORD[_i9]
	mov	ecx,	1
	div	ecx
	mov	DWORD[_i11],	eax
	mov	eax,	DWORD[_i11]
	mov	DWORD[val2],	eax
	push	_s8
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	DWORD[val2]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	push	_s9
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	val1
	push	formatIntIn
	call	_scanf
	add	esp,	0x08
	mov	edi,	DWORD[val1]
	imul	edi,	10
	mov	DWORD[_i12],	edi
	mov	eax,	DWORD[_i12]
	mov	DWORD[val1],	eax
	push	DWORD[val1]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
exit:
	mov	eax,	0x0
	call	_ExitProcess@4
