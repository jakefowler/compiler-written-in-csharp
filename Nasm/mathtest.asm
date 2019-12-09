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
	_s14:	db	"val1 = (10 * (11 + 12) / 13 + (14 * 15))",0x0d,0x0a,0
	_s0:	db	"Beginning Program",0x0d,0x0a,0
	_s5:	db	"val1 = 20 / 5",0x0d,0x0a,0
	_s1:	db	"val1 = 1 + 2 + 3 + 4",0x0d,0x0a,0
	_s3:	db	"val2 = 3 * 4",0x0d,0x0a,0
	_s8:	db	"val2 = 6 - 7 * (8+9)",0x0d,0x0a,0
;-----------------------------
; uninitialized data
;-----------------------------
section .bss USE32
	astring:	resb	128
	val2:	resd	1
	_temp2:	resd	1
	_temp13:	resd	1
	_temp7:	resd	1
	_temp4:	resd	1
	_temp12:	resd	1
	val1:	resd	1
	_temp10:	resd	1
	_temp9:	resd	1
	_temp6:	resd	1
	_temp11:	resd	1
;-----------------------------
; code
;-----------------------------
section .code USE32
_main:
	push	_s0
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	mov	esi,	1
	add	esi,	2
	add	esi,	3
	add	esi,	4
	mov	DWORD[val1],	esi
	push	_s1
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	DWORD[val1]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	mov	edi,	3
	imul	edi,	4
	mov	DWORD[_temp2],	edi
	mov	eax,	DWORD[_temp2]
	mov	DWORD[val2],	eax
	push	_s3
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	DWORD[val2]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	xor	edx,	edx
	mov	eax,	20
	mov	ecx,5
	div	ecx
	mov	DWORD[_temp4],	eax
	mov	eax,	DWORD[_temp4]
	mov	DWORD[val1],	eax
	push	_s5
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	DWORD[val1]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	mov	esi,	8
	add	esi,	9
	mov	DWORD[_temp6],	esi
	mov	edi,	7
	imul	edi,	DWORD[_temp6]
	mov	DWORD[_temp7],	edi
	mov	esi,	6
	sub	esi,	DWORD[_temp7]
	mov	DWORD[val2],	esi
	push	_s8
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	DWORD[val2]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
	mov	esi,	11
	add	esi,	12
	mov	DWORD[_temp9],	esi
	mov	edi,	10
	imul	edi,	DWORD[_temp9]
	mov	DWORD[_temp10],	edi
	xor	edx,	edx
	mov	eax,	DWORD[_temp10]
	mov	ecx,13
	div	ecx
	mov	DWORD[_temp11],	eax
	mov	edi,	14
	imul	edi,	15
	mov	DWORD[_temp12],	edi
	mov	esi,	DWORD[_temp11]
	add	esi,	DWORD[_temp12]
	mov	DWORD[_temp13],	esi
	mov	eax,	DWORD[_temp13]
	mov	DWORD[val1],	eax
	push	_s14
	push	stringPrinter
	call	_printf
	add	esp,	0x08
	push	DWORD[val1]
	push	numberPrinter
	call	_printf
	add	esp,	0x08
exit:
	mov	eax,	0x0
	call	_ExitProcess@4
