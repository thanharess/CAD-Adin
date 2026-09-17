;;; BM_Highlight_ByLayer_Color.lsp
;;; Highlight tất cả block cùng tên bằng cách chuyển layer và đổi màu layer tạm
;;; Cách dùng: APPLOAD, gõ BLHLAYER

(vl-load-com)

(defun bm-print (s) (princ (strcat "\n" s)))

(defun bm-get-block-name (/ ent ename edata blkname)
  (setq ent (entsel "\nChọn một block (INSERT) để highlight tất cả cùng loại: "))
  (if (not ent)
    (progn (bm-print "❌ Bạn đã hủy chọn.") nil)
    (progn
      (setq ename (car ent))
      (setq edata (entget ename))
      (if (and edata (= "INSERT" (cdr (assoc 0 edata))))
        (cdr (assoc 2 edata))
        (progn (bm-print "❌ Đối tượng không phải block (INSERT).") nil)
      )
    )
  )
)

(defun bm-get-highlight-color (/ k color)
  (initget "1 2 3 4")
  (setq k (getkword "\nChọn màu highlight (1=Đỏ, 2=Xanh lá, 3=Vàng, 4=Hồng) <1>: "))
  (if (not k) (setq k "1"))
  (setq color (atoi k))
  (cond
    ((= color 1) 1)   ; đỏ
    ((= color 2) 3)   ; xanh lá
    ((= color 3) 2)   ; vàng
    ((= color 4) 6)   ; hồng
    (T 1)
  )
)

(defun bm-ensure-layer (lname color / doc layers lay)
  (setq doc (vla-get-ActiveDocument (vlax-get-acad-object)))
  (setq layers (vla-get-layers doc))
  (if (not (tblobjname "LAYER" lname))
    (setq lay (vla-Add layers lname))
    (setq lay (vla-Item layers lname))
  )
  (vl-catch-all-apply 'vla-put-Color (list lay color))
  (if (vlax-property-available-p lay 'Locked)
    (vl-catch-all-apply 'vla-put-Locked (list lay :vlax-false))
  )
  lay
)


(defun bm-get-all-inserts-by-name (name)
  (ssget "X" (list (cons 0 "INSERT") (cons 2 name)))
)

(defun bm-move-to-layer-and-store (ss newlayer / n i en ed oldlayer-list oldlayer)
  (setq n (sslength ss) i 0 oldlayer-list '())
  (while (< i n)
    (setq en (ssname ss i))
    (setq ed (entget en))
    (setq oldlayer (cdr (assoc 8 ed)))
    (if (not oldlayer) (setq oldlayer "0"))
    (setq oldlayer-list (cons oldlayer oldlayer-list))
    (if (assoc 8 ed)
      (entmod (subst (cons 8 newlayer) (assoc 8 ed) ed))
      (entmod (append ed (list (cons 8 newlayer))))
    )
    (setq i (1+ i))
  )
  (reverse oldlayer-list)
)

(defun bm-restore-layers (ss oldlayer-list / n i en ed oldlayer)
  (setq n (sslength ss) i 0)
  (while (< i n)
    (setq en (ssname ss i))
    (setq ed (entget en))
    (setq oldlayer (nth i oldlayer-list))
    (if oldlayer
      (if (assoc 8 ed)
        (entmod (subst (cons 8 oldlayer) (assoc 8 ed) ed))
        (entmod (append ed (list (cons 8 oldlayer))))
      )
    )
    (setq i (1+ i))
  )
)

(defun bm-highlight-by-layer (blkname / ss cnt doc hl-lay lay-obj oldlayers color)
  (setq ss (bm-get-all-inserts-by-name blkname))
  (if (not ss)
    (bm-print "❌ Không tìm thấy block cùng tên trong bản vẽ.")
    (progn
      (setq cnt (sslength ss))
      (setq color (bm-get-highlight-color))
      (setq doc (vla-get-ActiveDocument (vlax-get-acad-object)))
      (setq hl-lay "BM_HIGHLIGHT")
      (setq lay-obj (bm-ensure-layer hl-lay color))
      (setq oldlayers (bm-move-to-layer-and-store ss hl-lay))
      (vla-regen doc acActiveViewport)
      (bm-print (strcat "🔍 Đã highlight " (itoa cnt) " block \"" blkname "\" bằng màu layer " (itoa color) "."))
      (bm-print "Nhấn Enter để khôi phục layer gốc...")
      (getstring)
      (bm-restore-layers ss oldlayers)
      (vla-regen doc acActiveViewport)
      (bm-print "✅ Đã khôi phục layer gốc.")
    )
  )
)

(defun c:BLHLAYER (/ blkname)
  (princ "\n=== BM_Highlight_ByLayer_Color ===")
  (setq blkname (bm-get-block-name))
  (if blkname
    (progn
      (bm-print (strcat "🔹 Block được chọn: " blkname))
      (bm-highlight-by-layer blkname)
    )
  )
  (princ)
)

(princ "\nGõ BLHLAYER để chạy lệnh.")
(princ)
